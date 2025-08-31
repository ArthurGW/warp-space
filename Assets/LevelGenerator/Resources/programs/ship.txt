%* Specification for the ship, i.e. the overall level shape *%

% All available squares
grid(1..width, 1..height).
same_square(X, Y, X, Y) :- grid(X, Y).

% Outer space - two triangular regions around the nose of the ship
in_space(X, Y) :- grid(X, Y), X <= height / 2, Y < height / 2 - X + 2.
in_space(X, Y) :- grid(X, Y), X <= height / 2, Y > height / 2 + X - 1.

% Also, all edges of the map, squares that are on the grid and border non-grid positions
in_space(X, Y) :- grid(X, Y), not grid(X,Y-1; X,Y+1; X-1,Y; X+1,Y).

% The ship's hull - all squares that border outer space, and are not space themselves
hull(X, Y) :- grid(X, Y), not in_space(X, Y), in_space(X,Y-1; X,Y+1; X-1,Y; X+1,Y).

% Finally, all remaining squares are internal ship squares
ship(X, Y) :- grid(X, Y), not in_space(X, Y), not hull(X, Y).

%* Rooms within the ship *%

% Rooms have sizes between 2x2 and 4x4, and must have all four corners within the ship
% The program is free to choose any number of rooms between min_rooms and max_rooms
min_rooms {
    room(XX, YY, W, H)
        : W=2..4, H=2..4,
          ship(XX, YY),
          ship(XX, YY + H - 1),
          ship(XX + W - 1, YY),
          ship(XX + W - 1, YY + H - 1)
} max_rooms.

% Corridors - at least 3, equivalent to single width & height rooms
3 { corridor(X, Y) : ship(X, Y) }.
room(X, Y, 1, 1) :- corridor(X, Y).

% A room defines an area of room_squares, each of which records their location and the room they are part of
room_square(X..(X + W - 1), Y..(Y + H - 1), X, Y, W, H) :- room(X, Y, W, H), W > 1, H > 1.
room_square(X, Y, X, Y, 1, 1) :- room(X, Y, 1, 1).

%* Alien breaches - with size 1x2 or 2x1 that start in space and penetrate the hull.
These are placed next to an existing non-corridor room, to ensure they are reachable. They are also placed only on
straight hull edges, for ease of placement in the game. The breached room is also recorded, for connecting up later. *%
num_breaches {
    % Vertical, from the top
    alien_breach(X, Y1, 1, 2, room(RX, RY, RW, RH))
        : in_space(X, Y1),
          hull(X, Y2),
          room_square(X, Y3, RX, RY, RW, RH),
          Y2 - Y1 = 1, Y3 - Y2 = 1,
          not corridor(X, Y3),
          ship(X+2, Y3), ship(X+1, Y3), ship(X-1, Y3), ship(X-2, Y3);
    % Vertical, from the bottom
    alien_breach(X, Y2, 1, 2, room(RX, RY, RW, RH))
        : in_space(X, Y1),
          hull(X, Y2),
          room_square(X, Y3, RX, RY, RW, RH),
          Y1 - Y2 = 1, Y2 - Y3 = 1,
          not corridor(X, Y3),
          ship(X+2, Y3), ship(X+1, Y3), ship(X-1, Y3), ship(X-2, Y3);
    % Horizontal, from the left
    alien_breach(X1, Y, 2, 1, room(RX, RY, RW, RH))
        : in_space(X1, Y),
          hull(X2, Y),
          room_square(X3, Y, RX, RY, RW, RH),
          X2 - X1 = 1, X3 - X2 = 1,
          not corridor(X3, Y),
          ship(X3, Y+2), ship(X3, Y+1), ship(X3, Y-1), ship(X3, Y-2);
    % Horizontal, from the right
    alien_breach(X2, Y, 2, 1, room(RX, RY, RW, RH))
        : in_space(X1, Y),
          hull(X2, Y),
          room_square(X3, Y, RX, RY, RW, RH),
          X1 - X2 = 1, X2 - X3 = 1,
          not corridor(X3, Y),
          ship(X3, Y+2), ship(X3, Y+1), ship(X3, Y-1), ship(X3, Y-2)
} num_breaches.
breach_square(X, Y, X, Y, 2, 1; X+1, Y, X, Y, 2, 1)
    :- alien_breach(X, Y, 2, 1, _).
breach_square(X, Y, X, Y, 1, 2; X, Y+1, X, Y, 1, 2)
    :- alien_breach(X, Y, 1, 2, _).

% Choose start and finish rooms that are not corridors, not breached, and are different to each other
1 { start_room(room(X, Y, W, H)) : room(X, Y, W, H), not corridor(X, Y), not alien_breach(_, _, _, _, room(X, Y, W, H)) } 1.
1 { finish_room(room(X, Y, W, H)) : room(X, Y, W, H), not corridor(X, Y), not alien_breach(_, _, _, _, room(X, Y, W, H)), not start_room(room(X, Y, W, H)) } 1.

%* Reachability determination *%

% Trace reachability from start room, which is always reachable
reachable(room(X, Y, W, H)) :- start_room(room(X, Y, W, H)).

% A room is reachable if it is next to another reachable room
reachable(room(X1, Y1, W1, H1)) :- room(X1, Y1, W1, H1), reachable(room(X2, Y2, W2, H2)), next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).

% The next_to relation is commutative
next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- next_to(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)).

% A room is next_to to another room if it borders it on the right hand or lower side
next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), X2 = X1 + W1, Y2 > Y1 - H2, Y2 < Y1 + H1.
next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), Y2 = Y1 + H1, X2 > X1 - W2, X2 < X1 + W1.

%* Constraints *%

% height must be an even number (i.e. height modulo 2 == 1 is not allowed), so there is a central nose point
:- height \ 2 = 1.

% No square can be part of more than one room (or breach)
:- 2 { room_square(X, Y, _, _, _, _); breach_square(X, Y, _, _, _, _)  }, grid(X, Y).

% No square made of 4 adjacent corridors can exist
:- corridor(X, Y), corridor(X+1, Y), corridor(X, Y+1), corridor(X+1, Y+1).

% No two breaches can be adjacent
:- breach_square(X, Y, BX1, BY1, _, _),
    breach_square(X + 1, Y, BX2, BY2, _, _),
    not same_square(BX1, BY1, BX2, BY2),
    grid(BX1, BY1), grid(BX2, BY2).
:- breach_square(X, Y, BX1, BY1, _, _),
    breach_square(X, Y + 1, BX2, BY2, _, _),
    not same_square(BX1, BY1, BX2, BY2),
    grid(BX1, BY1), grid(BX2, BY2).

% Every room must be reachable
:- room(X1, Y1, W1, H1), not reachable(room(X1, Y1, W1, H1)).

% Every (ship) row and column must have some part of a room in it (-4 to account for space and hull around ship)
% This reads as "count the number of X values that are both in a room and in a ship square, and reject if there are not
% enough of them (and the same for Y values)
:- #count { 1,X : room_square(X, _, _, _, _, _), ship(X, _) } < width - 4.
:- #count { 1,Y : room_square(_, Y, _, _, _, _), ship(_, Y) } < height - 4.

% No more than 2/3 of the (ship) map is filled (roughly, not accounting for nose section)
:- ((width - 4) * (height - 4) * 2 / 3) < { room_square(X, Y, _, _, _, _) : ship(X, Y) }.

%* Preferences *%

% Encourage forming more corridors
#maximize { 1@1,X,Y : corridor(X, Y) }.

%* Output predicates *%

#show ship/2.
#show in_space/2.
#show hull/2.
#show room_square/6.
#show breach_square/6.
#show corridor/2.
#show room/4.
#show alien_breach/5.
#show start_room/1.
#show finish_room/1.