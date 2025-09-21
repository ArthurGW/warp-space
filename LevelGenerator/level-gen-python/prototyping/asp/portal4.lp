% ADDED START FINISH NOT CONNECTED


%* Specification for the connections in the ship *%



%* Connectedness of rooms *%

% The next_to relation is commutative
next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- next_to(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)).

% A room is next_to to another room if it borders it on the right hand or lower side
next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), X2 = X1 + W1, Y2 > Y1 - H2, Y2 < Y1 + H1.
next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), Y2 = Y1 + H1, X2 > X1 - W2, X2 < X1 + W1.

% Choose an arbitrary set of next_to *rooms* to physically connect (corridors are always connected to adjacent corridors)
0 { connected(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) } 1
    :- next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)),
       not connected(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)).
connected(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2))
    :- next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)),
%        corridor(X1, Y1), corridor(X2, Y2).
       W1 = W2, W2 = 1,
       not connected(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)).

% Choose num_portals rooms to connect by portal, that are not the same room or next to each other, and not corridors
num_portals {
    portal(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2))
        : room(X1, Y1, W1, H1),
          room(X2, Y2, W2, H2),
          W1 != 1, W2 != 1,
%          not corridor(X1, Y1), not corridor(X2, Y2),
          not same_square(X1, Y1, X2, Y2),
%          not next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)),
          not portal(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1))
} num_portals.

%* Reachability determination *%

% There is a relation adjacent that is commutative
%adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), P) :- adjacent(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1), P).

% Rooms are "adjacent" to each other in a graph sense, if they are connected by portal or physically
%adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), 1)
%    :- portal(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).
%adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), 0) :- connected(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).

% Trace reachability from start room, which is always reachable
full_reachable(room(X, Y, W, H)) :- start_room(room(X, Y, W, H)).

% A room is reachable if it is adjacent to another reachable room
full_reachable(room(X1, Y1, W1, H1))
    :- room(X1, Y1, W1, H1),
    full_reachable(room(X2, Y2, W2, H2)),
    connected(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1); room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).
%    adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), _).

full_reachable(room(X1, Y1, W1, H1))
    :- room(X1, Y1, W1, H1),
    full_reachable(room(X2, Y2, W2, H2)),
    portal(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1); room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).

%* Constraints *%

% Every room must be reachable
:- room(X1, Y1, W1, H1), not full_reachable(room(X1, Y1, W1, H1)).

% The start room and finish room cannot be adjacent
:- connected(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2); room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)),
    start_room(room(X1, Y1, W1, H1)), finish_room(room(X2, Y2, W2, H2)).
:- portal(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2); room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)),
    start_room(room(X1, Y1, W1, H1)), finish_room(room(X2, Y2, W2, H2)).

% At least one breached room must be connected physically to another room
:- { connected(room(RX, RY, RW, RH), _) : alien_breach(_, _, _, _, room(RX, RY, RW, RH));
    connected(_, room(RX, RY, RW, RH)) : alien_breach(_, _, _, _, room(RX, RY, RW, RH))
} 0, num_portals > 0.
%    connected(_, room(RX, RY, RW, RH)) : alien_breach(_, _, _, _, room(RX, RY, RW, RH))
%:- {
%    adjacent(room(RX, RY, RW, RH), _, 0) : alien_breach(_, _, _, _, room(RX, RY, RW, RH))
%} 0, num_portals > 0.

%* Preferences *%

% Discourage having too many connected rooms, i.e. not every pair of rooms that are next to each other should be joined
%#minimize { 1@1,X1,Y1,X2,Y2 : connected(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) }.
#minimize { (W1+W2-2)@1,X1,Y1,X2,Y2 : connected(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) }.

%* Output predicates *%

%#show adjacent/3.
#show connected/2.
#show portal/2.
