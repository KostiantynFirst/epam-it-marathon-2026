@ui @group-pre-order
Feature: Group pre-order on the storefront

  The host's side of a group dinner, end to end: guests join through the invite and
  fill their carts (through the service, as they would on their own phones), while the
  host reviews everything on the storefront, removes a participant and sends one
  consolidated pre-order to the restaurant. The waiter then sees exactly what the host
  sent. The service-level rules are in "Group pre-order rules".

  Background:
    Given the restaurant has a waiter account
    And a host has booked a table for 6 guests and shared the invite

  Rule: The host sees every participant, their dishes and totals

    @level-1 @smoke
    Scenario: The host reviews the group's carts
      Given "Andrii" has joined the group
      And "Andrii" has put 2 of the first dish in the cart
      And "Andrii" has confirmed the cart
      And "Olena" has joined the group
      And "Olena" has put 1 of the second dish in the cart
      When the host opens the group pre-order
      Then the host sees these participants:
        | participant | cart           | subtotal |
        | Andrii      | Submitted      | $25.00   |
        | Olena       | Still choosing | $8.00    |
      And the grand total is "$25.00"

    @level-1
    Scenario: The host sees a guest's confirmation without leaving the screen
      Given "Andrii" has joined the group
      And "Andrii" has put 2 of the first dish in the cart
      And the host has opened the group pre-order
      When "Andrii" confirms the cart
      And the host refreshes the group pre-order
      Then the host sees these participants:
        | participant | cart      | subtotal |
        | Andrii      | Submitted | $25.00   |
      And the grand total is "$25.00"

    @level-1
    Scenario: The host is told when nobody has joined yet
      When the host opens the group pre-order
      Then the host is told that nobody has added any dishes yet

  Rule: The host sends one consolidated pre-order to the restaurant

    @level-2 @smoke
    Scenario: Same dishes are merged and unconfirmed carts are left out
      Given "Andrii" has joined the group
      And "Andrii" has put 2 of the first dish in the cart
      And "Andrii" has confirmed the cart
      And "Olena" has joined the group
      And "Olena" has put 1 of the first dish in the cart
      And "Olena" has confirmed the cart
      And "Maria" has joined the group
      And "Maria" has put 1 of the second dish in the cart
      And the host has opened the group pre-order
      When the host sends the pre-order to the restaurant
      Then the pre-order is marked as sent to the restaurant
      And the consolidated pre-order is:
        | dish       | quantity |
        | first dish | 3        |
      And the grand total is "$37.50"
      And the waiter sees the order, sent by the customer, with:
        | dish        | quantity |
        | host's dish | 1        |
        | first dish  | 3        |

  Rule: The host can remove a participant

    @level-3
    Scenario: The host removes a guest and the totals follow
      Given "Andrii" has joined the group
      And "Andrii" has put 2 of the first dish in the cart
      And "Andrii" has confirmed the cart
      And "Olena" has joined the group
      And "Olena" has put 1 of the second dish in the cart
      And "Olena" has confirmed the cart
      And the host has opened the group pre-order
      When the host removes "Olena" from the group
      Then the host sees these participants:
        | participant | cart      | subtotal |
        | Andrii      | Submitted | $25.00   |
      And the grand total is "$25.00"

    @level-3 @smoke @e2e
    Scenario: From the guests' carts to the waiter, without the removed guest
      Given "Andrii" has joined the group
      And "Andrii" has put 2 of the first dish in the cart
      And "Andrii" has confirmed the cart
      And "Olena" has joined the group
      And "Olena" has put 1 of the first dish in the cart
      And "Olena" has put 1 of the second dish in the cart
      And "Olena" has confirmed the cart
      And "Maria" has joined the group
      And "Maria" has put 1 of the second dish in the cart
      And "Maria" has confirmed the cart
      And the host has opened the group pre-order
      When the host removes "Olena" from the group
      And the host sends the pre-order to the restaurant
      Then the pre-order is marked as sent to the restaurant
      And the consolidated pre-order is:
        | dish        | quantity |
        | first dish  | 2        |
        | second dish | 1        |
      And the waiter sees the order, sent by the customer, with:
        | dish        | quantity |
        | host's dish | 1        |
        | first dish  | 2        |
        | second dish | 1        |
