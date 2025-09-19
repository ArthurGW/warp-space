% CHANGE TO NON-NESTED ATOMS, REMOVED FANCY CONNECTION MIN
% USES A LOT LESS MEMORY

%* Specification for the connections in the ship *%

%* Connectedness of rooms *%

% The next_to relation is commutative
next_to(X1, Y1, X2, Y2) :- next_to(X2, Y2, X1, Y1).

% A room is next_to to another room if it borders it on the right hand or lower side
next_to(X1, Y1, X2, Y2) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), X2 = X1 + W1, Y2 > Y1 - H2, Y2 < Y1 + H1.
next_to(X1, Y1, X2, Y2) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), Y2 = Y1 + H1, X2 > X1 - W2, X2 < X1 + W1.

% Choose an arbitrary set of next_to *rooms* to physically connect (corridors are always connected to adjacent corridors)
0 { connected(X1, Y1, X2, Y2) } 1
    :- next_to(X1, Y1, X2, Y2),
       not connected(X2, Y2, X1, Y1).
connected(X1, Y1, X2, Y2)
    :- next_to(X1, Y1, X2, Y2),
       corridor(X1, Y1),
       corridor(X2, Y2),
%       W1 = W2, W2 = 1,
%       room(X1, Y1, W1, H1), room(X2, Y2, W2, H2),
       not connected(X2, Y2, X1, Y1).

% Choose num_portals rooms to connect by portal, that are not the same room or next to each other, and not corridors
num_portals {
    portal(X1, Y1, X2, Y2)
        : room(X1, Y1, W1, H1),
          room(X2, Y2, W2, H2),
          W1 != 1, W2 != 1,
%          not corridor(X1, Y1), not corridor(X2, Y2),
          not same_square(X1, Y1, X2, Y2),
%          not next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)),
          not portal(X2, Y2, X1, Y1)
} num_portals.

%* Reachability determination *%

% There is a relation adjacent that is commutative
%adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), P) :- adjacent(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1), P).

% Rooms are "adjacent" to each other in a graph sense, if they are connected by portal or physically
%adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), 1)
%    :- portal(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).
%adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), 0) :- connected(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).

% Trace reachability from start room, which is always reachable
full_reachable(X, Y) :- start_room(X, Y).

% A room is reachable if it is adjacent to another reachable room
full_reachable(X1, Y1)
    :- room(X1, Y1, _, _),
    full_reachable(X2, Y2),
    connected(X2, Y2, X1, Y1; X1, Y1, X2, Y2).
%    adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), _).

full_reachable(X1, Y1)
    :- room(X1, Y1, W1, H1),
    full_reachable(X2, Y2),
    portal(X2, Y2, X1, Y1; X1, Y1, X2, Y2).

%* Constraints *%

% Every room must be reachable
:- room(X1, Y1, _, _), not full_reachable(X1, Y1).

% The start room and finish room cannot be adjacent
:- connected(X2, Y2, X1, Y1; X1, Y1, X2, Y2),
    start_room(X1, Y1), finish_room(X2, Y2).
:- portal(X2, Y2, X1, Y1; X1, Y1, X2, Y2),
    start_room(X1, Y1), finish_room(X2, Y2).

% At least one breached room must be connected physically to another room
:- { connected(RX, RY, _, _) : alien_breach(_, _, _, _, RX, RY);
    connected(_, _, RX, RY) : alien_breach(_, _, _, _, RX, RY)
} 0, num_portals > 0.
%    connected(_, room(RX, RY, RW, RH)) : alien_breach(_, _, _, _, room(RX, RY, RW, RH))
%:- {
%    adjacent(room(RX, RY, RW, RH), _, 0) : alien_breach(_, _, _, _, room(RX, RY, RW, RH))
%} 0, num_portals > 0.

%* Preferences *%

% Discourage having too many connected rooms, i.e. not every pair of rooms that are next to each other should be joined
%#minimize { 1@1,X1,Y1,X2,Y2 : connected(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) }.
#minimize { 1@1,X1,Y1,X2,Y2 : connected(X1, Y1, X2, Y2) }.

%* Output predicates *%

%#show adjacent/3.
#show connected/4.
#show portal/4.
