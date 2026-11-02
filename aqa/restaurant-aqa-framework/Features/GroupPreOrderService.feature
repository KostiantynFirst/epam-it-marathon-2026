@api @group-pre-order @sequential
Feature: Group pre-order rules

  A host books a table for a group and shares an invite. Every guest who joins fills a
  cart of their own and confirms it when ready. The host sees everyone's choices, can
  remove a participant, and sends one consolidated pre-order to the restaurant, which
  the waiter then works with.

  These scenarios check the rules through the service: who may see and change the
  group, what is counted, and how each refusal is explained. The host's journey on the
  storefront is in "Group pre-order on the storefront".

  Background:
    Given the restaurant has a waiter account
    And a host has booked a table for 6 guests and shared the invite

  Rule: The host sees every participant, their dishes and totals

    @level-1 @smoke
    Scenario: The host sees confirmed carts, unconfirmed carts and empty carts alike
      Given "Andrii" has joined the group
      And "Andrii" has put 2 of the first dish in the cart
      And "Andrii" has confirmed the cart
      And "Olena" has joined the group
      And "Olena" has put 1 of the second dish in the cart
      And "Maria" has joined the group
      When the host opens the group pre-order
      Then the host sees these participants:
        | participant | dishes | total | confirmed |
        | Andrii      | 2      | 25.00 | yes       |
        | Olena       | 1      | 8.00  | no        |
        | Maria       | 0      | 0.00  | no        |
      And the group total is 33.00

    @level-1
    Scenario: The group total follows a guest's cart
      Given "Andrii" has joined the group
      And "Andrii" has put 1 of the first dish in the cart
      And the host has opened the group pre-order
      When "Andrii" puts 2 of the second dish in the cart
      And the host refreshes the group pre-order
      Then the group total is 28.50

    @level-1
    Scenario: A group nobody has joined yet is empty
      When the host opens the group pre-order
      Then nobody is in the group

  Rule: Only the host can see the group pre-order

    @level-1 @negative
    Scenario Outline: The <party> cannot see the group pre-order
      Given "Andrii" has joined the group
      When the <party> tries to open the group pre-order
      Then the <party> is turned away

      Examples: Everyone but the host
        | party                        |
        | another customer             |
        | guest                        |
        | waiter                       |
        | visitor who is not signed in |

    @level-1 @negative
    Scenario: A group pre-order that does not exist cannot be opened
      When the host opens a group pre-order that does not exist
      Then the host is told that the group could not be found

  Rule: The host sends one consolidated pre-order of the confirmed carts

    @level-2 @smoke
    Scenario: Confirmed carts are merged by dish; unconfirmed carts are left out
      Given "Andrii" has joined the group
      And "Andrii" has put 2 of the first dish in the cart
      And "Andrii" has confirmed the cart
      And "Olena" has joined the group
      And "Olena" has put 1 of the first dish in the cart
      And "Olena" has confirmed the cart
      And "Maria" has joined the group
      And "Maria" has put 1 of the second dish in the cart
      When the host sends the pre-order to the restaurant
      Then the restaurant receives:
        | dish       | quantity |
        | first dish | 3        |
      And the waiter sees the order, sent by the customer, with:
        | dish        | quantity |
        | host's dish | 1        |
        | first dish  | 3        |

    @level-2 @negative
    Scenario: Nothing can be sent while nobody has confirmed a cart
      Given "Andrii" has joined the group
      And "Andrii" has put 1 of the first dish in the cart
      When the host tries to send the pre-order to the restaurant
      Then the host is told "There are no submitted guest sessions."

    @level-2 @negative
    Scenario: Nothing can be sent when the booking has no order to add the dishes to
      Given a host has booked a table for 6 guests without a pre-order of their own
      And "Andrii" has joined the group
      And "Andrii" has put 1 of the first dish in the cart
      And "Andrii" has confirmed the cart
      When the host tries to send the pre-order to the restaurant
      Then the host is told "was not found"

    @level-2 @negative
    Scenario: The pre-order of a group that does not exist cannot be sent
      When the host tries to send the pre-order of a group that does not exist
      Then the host is told that the group could not be found

    @level-2 @negative
    Scenario: The pre-order for a booking that does not exist cannot be sent
      When the host tries to send the pre-order for a booking that does not exist
      Then the host is told that the booking could not be found

    @level-2 @negative
    Scenario: A pre-order that was already sent cannot be sent again
      Given "Andrii" has joined the group
      And "Andrii" has put 1 of the first dish in the cart
      And "Andrii" has confirmed the cart
      And the host has sent the pre-order to the restaurant
      When the host tries to send the pre-order to the restaurant again
      Then the host is told "Group reservation was submitted."

    @level-2 @negative
    Scenario Outline: The <party> cannot send the pre-order
      Given "Andrii" has joined the group
      And "Andrii" has put 1 of the first dish in the cart
      And "Andrii" has confirmed the cart
      When the <party> tries to send the pre-order to the restaurant
      Then the <party> is turned away

      Examples: Everyone but the host
        | party                        |
        | another customer             |
        | guest                        |
        | waiter                       |
        | visitor who is not signed in |

  Rule: The host can remove a participant of their own group

    @level-3 @smoke
    Scenario: Removing one of several participants recalculates the total
      Given "Andrii" has joined the group
      And "Andrii" has put 2 of the first dish in the cart
      And "Olena" has joined the group
      And "Olena" has put 1 of the second dish in the cart
      When the host removes "Olena" from the group
      Then the host sees these participants:
        | participant | dishes | total | confirmed |
        | Andrii      | 2      | 25.00 | no        |
      And the group total is 25.00

    @level-3
    Scenario: A removed participant's confirmed cart is no longer sent to the restaurant
      Given "Andrii" has joined the group
      And "Andrii" has put 1 of the first dish in the cart
      And "Andrii" has confirmed the cart
      And "Olena" has joined the group
      And "Olena" has put 2 of the second dish in the cart
      And "Olena" has confirmed the cart
      When the host removes "Olena" from the group
      And the host sends the pre-order to the restaurant
      Then the restaurant receives:
        | dish       | quantity |
        | first dish | 1        |

    @level-3
    Scenario: Removing the last participant leaves the group empty
      Given "Andrii" has joined the group
      And "Andrii" has put 1 of the first dish in the cart
      When the host removes "Andrii" from the group
      Then nobody is in the group

    @level-3 @negative
    Scenario: A participant of another group cannot be removed
      Given the host has also booked a table for 4 guests for another party, "Book club"
      And "Ivan" has joined the group of "Book club"
      When the host tries to remove "Ivan" from the first group
      Then the host is told "Guest session does not belong to this group reservation."

    @level-3 @negative
    Scenario: Someone who never joined cannot be removed
      When the host tries to remove someone who never joined the group
      Then the host is told that the participant could not be found

    @level-3 @negative
    Scenario Outline: The <party> cannot remove a participant
      Given "Andrii" has joined the group
      When the <party> tries to remove "Andrii" from the group
      Then the <party> is turned away

      Examples: Everyone but the host
        | party                        |
        | another customer             |
        | guest                        |
        | waiter                       |
        | visitor who is not signed in |
