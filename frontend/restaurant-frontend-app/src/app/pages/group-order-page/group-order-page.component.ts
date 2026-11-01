import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { forkJoin, switchMap } from 'rxjs';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { GroupReservationStatus } from '@app/models/group-orders/group-status.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';

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
          this.loading.set(false);
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
}