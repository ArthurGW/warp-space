%* Specification for the ship, i.e. the overall level shape *%

% All available squares
grid(1..width, 1..height).

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
%  The program is free to choose any number of rooms between min_rooms and max_rooms
min_rooms { room(XX, YY, W, H)
    : W=2..4, H=2..4,
    ship(XX, YY),
    ship(XX, YY + H - 1),
    ship(XX + W - 1, YY),
    ship(XX + W - 1, YY + H - 1) } max_rooms.

% A room defines an area of room_squares, each of which records their location and the room they are part of
room_square(X..(X + W - 1), Y..(Y + H - 1), X, Y, W, H) :- room(X, Y, W, H), W > 1, H > 1.

% Corridors - at least 3, equivalent to single width & height rooms
3 { corridor(X, Y) : ship(X, Y) }.
room(X, Y, 1, 1) :- corridor(X, Y).
room_square(X, Y, X, Y, 1, 1) :- room(X, Y, 1, 1).

% Choose a start and a finish room
1 { start_room(room(X, Y, W, H)) : room(X, Y, W, H), W > 1, H > 1 } 1.
1 { finish_room(room(X, Y, W, H)) : room(X, Y, W, H), W > 1, H > 1 } 1.

%* Reachability determination *%

% Trace reachability from the start room - start with it as reachable
reachable(room(X, Y, W, H)) :- start_room(room(X, Y, W, H)).

% A room is reachable if it is adjacent to another reachable room
reachable(room(X1, Y1, W1, H1)) :- room(X1, Y1, W1, H1), reachable(room(X2, Y2, W2, H2)), adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).

% The adjacent relation is commutative
adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- adjacent(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)).

% A room is next_to to another room if it borders it on the right hand or lower side
next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), X2 = X1 + W1, Y2 > Y1 - H2, Y2 < Y1 + H1.
next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), Y2 = Y1 + H1, X2 > X1 - W2, X2 < X1 + W1.

% Choose an arbitrary set of next_to *rooms* to connect, i.e. make adjacent, but keep adjacent corridors connected always
% Since adjacent is commutative, these connections go both ways
0 { adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) } 1 :- next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).
adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- next_to(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)), W1 = W2, W2 = 1.

%* Constraints *%

% height must be an even number (i.e. height modulo 2 == 1 is not allowed), so there is a central nose point
:- height \ 2 = 1.

% No ship square can be part of more than one room
:- 2 { room_square(X, Y, _, _, _, _)  }, ship(X, Y).

% No square made of 4 adjacent corridors can exist
:- corridor(X, Y), corridor(X+1, Y), corridor(X, Y+1), corridor(X+1, Y+1).

% Every room must be reachable
:- room(X1, Y1, W1, H1), not reachable(room(X1, Y1, W1, H1)).

% Every (ship) row and column must have some part of a room in it (-4 to account for space and hull around ship)
% This reads as "count the number of X values that are both in a room and in a ship square, and reject if there are not
% enough of them (and the same for Y values)
:- #count { 1,X : room_square(X, _, _, _, _, _), ship(X, _) } < width - 4.
:- #count { 1,Y : room_square(_, Y, _, _, _, _), ship(_, Y) } < height - 4.

% No more than 3/4 of the (ship) map is filled (roughly, not accounting for nose section)
:- ((width - 5) * (height - 4) * 3 / 4) < { room_square(X, Y, _, _, _, _) : ship(X, Y) }.

% The start and finish rooms must be different
:- start_room(room(X, Y, W, H)), finish_room(room(X, Y, W, H)).

%* Preferences *%

% Encourage forming more corridors
#maximize { 1@1,X,Y : corridor(X, Y) }.

% Discourage having too many adjacent rooms, i.e. not every pair of rooms that are next to each other should be joined
#minimize { 1@1,X1,Y1,W1,H1,X2,Y2,W2,H2 : adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) }.

%* Level solution *%

% Path from start to other rooms
room_count(room(X, Y, W, H), 1) :- start_room(room(X, Y, W, H)).
room_count(room(X1, Y1, W1, H1), N+1) :- room(X1, Y1, W1, H1), not corridor(X1, Y1),
    N = #min { N2,X2,Y2: room_count(room(X2, Y2, W2, H2), N2), adjacent(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)) }.
room_count(room(X1, Y1, W1, H1), N) :- room(X1, Y1, W1, H1), corridor(X1, Y1),
    N = #min { N2,X2,Y2: room_count(room(X2, Y2, W2, H2), N2), adjacent(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)) }.

%% Rooms (including corridors) and their connections define the level's design
%__level_design(room(X,Y,W,H)) :- room(X,Y,W,H).
%__level_design(adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2))) :- adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).
%
%% The path from start room to finish room must be at least min_rooms long.
%__concept :- room_count(room(X, Y, W, H), N), finish_room(room(X, Y, W, H)), N >= min_rooms.

%* Output predicates *%

#show ship/2.
#show in_space/2.
#show hull/2.
#show room_square/6.
#show corridor/2.
%#show initial_room/4.
%#show reachable/1.
#show room/4.
#show adjacent/2.
#show start_room/1.
#show finish_room/1.
#show room_count/2.
