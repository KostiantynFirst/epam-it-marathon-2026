import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, Observable, Subscription, catchError, exhaustMap, forkJoin, interval, switchMap, tap } from 'rxjs';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { GroupReservationStatus } from '@app/models/group-orders/group-status.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';

/** Як часто підтягувати свіжі кошики, поки група Open. */
const SYNC_INTERVAL_MS = 15_000;

@Component({
  selector: 'app-group-order-page',
  imports: [ParticipantCardComponent, ModalComponent, CurrencyPipe, DatePipe],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent implements OnInit {
  private readonly groupOrders = inject(GroupOrdersService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reservationId = inject(ActivatedRoute).snapshot.paramMap.get('reservationId') ?? '';

  // --- стан екрана ---
  readonly loading = signal(true);
  readonly error = signal<'not-found' | 'failed' | null>(null);
  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly ownerSessionId = signal('');
  readonly status = signal<GroupReservationStatus>('Open');
  readonly confirmOpen = signal(false);
  readonly submitting = signal(false);
  readonly lastUpdated = signal<Date | null>(null);
  readonly refreshFailed = signal(false);
  readonly refreshing = signal(false);

  // бонус: кого зараз видаляємо (відкритий діалог) і чи йде запит
  readonly removeTarget = signal<Participant | null>(null);
  readonly removing = signal(false);

  private syncSubscription: Subscription | null = null;

  // --- похідні значення ---
  /** У замовлення ресторану йдуть лише підтверджені кошики. */
  readonly consolidated = computed(() => consolidatePreOrder(this.participants()));
  readonly grandTotal = computed(() => this.consolidated().total);
  readonly isSubmitted = computed(() => this.status() !== 'Open');
  readonly isEmpty = computed(() => this.participants().every((person) => person.items.length === 0));
  readonly pendingCount = computed(() => this.participants().filter((person) => !person.isSubmitted).length);
  readonly pendingMessage = computed(() => {
    const count = this.pendingCount();
    return count === 1
      ? `1 participant hasn't submitted their cart yet — their dishes won't be included in the order`
      : `${count} participants haven't submitted their cart yet — their dishes won't be included in the order`;
  });
  readonly canSubmit = computed(
    () => !this.isSubmitted() && !this.submitting() && this.consolidated().items.length > 0,
  );
  readonly guestsLabel = computed(
    () => `${this.participants().length} of ${this.invite()?.MaxGuestCount ?? 0} guests`,
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.stopSync(); // «Try again» не запускає другий таймер
    this.loading.set(true);
    this.error.set(null);

    this.groupOrders
      .getGroupInvite$(this.reservationId)
      .pipe(
        switchMap((invite) => {
          this.invite.set(invite);
          return forkJoin({
            sessions: this.groupOrders.getSessions$(invite.GroupReservationId),
            ownerSessionId: this.groupOrders.getOwnerSessionId$(this.reservationId),
            status: this.groupOrders.getStatus$(invite.GroupReservationId),
          });
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ sessions, ownerSessionId, status }) => {
          this.participants.set(sessions.participants);
          this.ownerSessionId.set(ownerSessionId);
          this.status.set(status.status);
          this.lastUpdated.set(new Date());
          this.loading.set(false);
          this.startSync();
        },
        error: (error: unknown) => {
          const notFound = error instanceof HttpErrorResponse && error.status === 404;
          this.error.set(notFound ? 'not-found' : 'failed');
          this.loading.set(false);
        },
      });
  }

  openConfirm(): void {
    this.confirmOpen.set(true);
  }

  closeConfirm(): void {
    if (!this.submitting()) {
      this.confirmOpen.set(false);
    }
  }

  submit(): void {
    const invite = this.invite();
    if (!invite || this.submitting()) {
      return; // захист від подвійного кліку
    }
    this.submitting.set(true);

    this.groupOrders
      .submit$(invite.GroupReservationId, this.reservationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.status.set('Submitted');
          this.stopSync();
          this.submitting.set(false);
          this.confirmOpen.set(false);
          this.toast.success('Pre-order sent to the restaurant');
        },
        error: () => {
          // toast з помилкою вже показав tokenInterceptor
          this.submitting.set(false);
          this.confirmOpen.set(false);
        },
      });
  }

  openRemove(person: Participant): void {
    this.removeTarget.set(person);
  }

  closeRemove(): void {
    if (!this.removing()) {
      this.removeTarget.set(null);
    }
  }

  confirmRemove(): void {
    const person = this.removeTarget();
    const invite = this.invite();
    if (!person || !invite || this.removing()) {
      return;
    }
    this.removing.set(true);

    this.groupOrders
      .removeSession$(invite.GroupReservationId, person.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (sessions) => {
          // список і суми оновлюються з відповіді сервера — через ті самі computed()
          this.participants.set(sessions.participants);
          this.removing.set(false);
          this.removeTarget.set(null);
        },
        error: () => {
          // toast з помилкою показав tokenInterceptor, список лишається як був
          this.removing.set(false);
          this.removeTarget.set(null);
        },
      });
  }

  /** Кнопка «Refresh» — синхронізуватись зараз. */
  refresh(): void {
    if (this.refreshing()) {
      return; // попередній запит ще не відповів
    }
    this.refreshing.set(true);
    this.fetchSilently$()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.refreshing.set(false),
        error: () => this.refreshing.set(false),
      });
  }

  private startSync(): void {
    if (this.isSubmitted()) {
      return;
    }
    this.syncSubscription = interval(SYNC_INTERVAL_MS)
      .pipe(
        // поки попередній запит не відповів, новий не стартує
        exhaustMap(() => this.fetchSilently$()),
        takeUntilDestroyed(this.destroyRef), // вихід зі сторінки зупиняє таймер
      )
      .subscribe({ error: () => undefined });
  }

  private stopSync(): void {
    this.syncSubscription?.unsubscribe();
    this.syncSubscription = null;
  }

  /** Тихе фонове оновлення: без лоадера і без toast; при помилці лишаємо старі дані. */
  private fetchSilently$(): Observable<unknown> {
    const groupId = this.invite()?.GroupReservationId ?? '';
    return forkJoin({
      sessions: this.groupOrders.getSessions$(groupId, { silent: true }),
      status: this.groupOrders.getStatus$(groupId, { silent: true }),
    }).pipe(
      tap({
        next: ({ sessions, status }) => {
          this.participants.set(sessions.participants);
          this.status.set(status.status);
          this.lastUpdated.set(new Date());
          this.refreshFailed.set(false);
          if (status.status !== 'Open') {
            this.stopSync();
          }
        },
        error: () => this.refreshFailed.set(true),
      }),
      // помилку вже показали рядком «Couldn't refresh», таймер має жити далі
      catchError(() => EMPTY),
    );
  }
}