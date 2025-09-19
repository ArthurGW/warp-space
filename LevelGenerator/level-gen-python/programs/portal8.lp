% MOVED DOUBLE-CONNECTION CONSTRAINTS TO SEPARATE CHECKS

%* Specification for the connections in the ship *%

%* Connectedness of rooms *%

% A room is next_to to another room if it borders it on the right hand or lower side
next_to(X1, Y1, X2, Y2) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), X2 = X1 + W1, Y2 > Y1 - H2, Y2 < Y1 + H1.
next_to(X1, Y1, X2, Y2) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), Y2 = Y1 + H1, X2 > X1 - W2, X2 < X1 + W1.

% The next_to relation is commutative
next_to(X1, Y1, X2, Y2) :- next_to(X2, Y2, X1, Y1).

% Choose an arbitrary set of next_to *rooms* to physically connect (corridors are always connected to adjacent corridors)
0 { connected(X1, Y1, X2, Y2) } 1
    :- next_to(X1, Y1, X2, Y2).
connected(X1, Y1, X2, Y2)
    :- next_to(X1, Y1, X2, Y2),
       corridor(X1, Y1),
       corridor(X2, Y2).

:- connected(X1, Y1, X2, Y2),
   connected(X2, Y2, X1, Y1).

% Choose num_portals rooms to connect by portal, that are not the same room or next to each other, and not corridors
num_portals {
    portal(X1, Y1, X2, Y2)
        : room(X1, Y1, W1, H1),
          room(X2, Y2, W2, H2),
          W1 != 1, W2 != 1,
          not same_square(X1, Y1, X2, Y2),
          not portal(X2, Y2, X1, Y1)
} num_portals.

:- portal(X1, Y1, X2, Y2),
   portal(X2, Y2, X1, Y1).

%* Reachability determination *%

% Trace reachability from start room, which is always reachable
reachable(X, Y) :- start_room(X, Y).

% A room is reachable if it is adjacent to another reachable room
reachable(X1, Y1)
    :- room(X1, Y1, _, _),
    reachable(X2, Y2),
    connected(X2, Y2, X1, Y1; X1, Y1, X2, Y2).

reachable(X1, Y1)
    :- room(X1, Y1, W1, H1),
    reachable(X2, Y2),
    portal(X2, Y2, X1, Y1; X1, Y1, X2, Y2).

%* Constraints *%

% Every room must be reachable
:- room(X1, Y1, _, _), not reachable(X1, Y1).

% The start room and finish room cannot be adjacent
:- connected(X2, Y2, X1, Y1; X1, Y1, X2, Y2),
    start_room(X1, Y1), finish_room(X2, Y2).
:- portal(X2, Y2, X1, Y1; X1, Y1, X2, Y2),
    start_room(X1, Y1), finish_room(X2, Y2).

% At least one breached room must be connected physically to another room
:- {
    connected(RX, RY, _, _) : alien_breach(_, _, _, _, RX, RY);
    connected(_, _, RX, RY) : alien_breach(_, _, _, _, RX, RY)
} 0, num_portals > 0.

%* Preferences *%

% Discourage having too many connected rooms, i.e. not every pair of rooms that are next to each other should be joined
% The particular formulation means corridor-corridor connections have zero cost, wider rooms have higher cost
#minimize { (W1+W2-2)@1,X1,Y1,X2,Y2
    : connected(X1, Y1, X2, Y2),
      room(X1, Y1, W1, _),
      room(X2, Y2, W2, _)
}.

%* Output predicates *%

#show connected/4.
#show portal/4.
