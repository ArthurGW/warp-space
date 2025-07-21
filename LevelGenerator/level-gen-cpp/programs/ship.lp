% Specification for the ship, i.e. the overall level shape.

grid(1..width, 1..height).

ship(X, Y) :- grid(X, Y), X != 1, X != width, Y != 1, Y != width.
hull(X, Y) :- grid(X, Y), not ship(X, Y).

% Rooms within the ship.

%2 { room(XX, YY, W, H) : ship(XX + W - 1, YY + H - 1), ship(XX, YY) } 4 :- W=2..3, H=2..3.
%1 { room(XX, YY, W, H) : ship(XX + W - 1, YY + H - 1), ship(XX, YY) } 1 :- W=4, H=4.
8 { room(XX, YY, W, H) : W=2..4, H=2..4, ship(XX + W - 1, YY + H - 1), ship(XX, YY) } 8.

room_square(X..(X + W - 1), Y..(Y + H - 1), X, Y, W, H) :- room(X, Y, W, H), W > 1, H > 1.
room_square(X, Y, X, Y, 1, 1) :- room(X, Y, 1, 1).

% Corridors - at least 3, equivalent to single width & height rooms

3 { corridor(X, Y) : ship(X, Y) } 5.
room(X, Y, 1, 1) :- corridor(X, Y).

%* Reachability *%

% Arbitrarily pick a room to start from - minimum row in Y, minimum X within that row
min_room_Y(YY) :- YY = #min { Y : room(_, Y, _, _); Y : corridor(X, Y) }.
min_room_X(XX) :- XX = #min { X : room(X, Y, _, _), min_room_Y(Y); X : corridor(X, Y), min_room_Y(Y)  }.
min_room(X, Y) :- min_room_X(X), min_room_Y(Y).

% Now trace reachability from there - start with the arbitrary room as reachable
reachable(room(X, Y, W, H)) :- min_room(X, Y), room(X, Y, W, H).

% A room is reachable if it is adjacent to another reachable room
reachable(room(X1, Y1, W1, H1)) :- room(X1, Y1, W1, H1), reachable(room(X2, Y2, W2, H2)), adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)).

% The adjacent relation is commutative
adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- adjacent(room(X2, Y2, W2, H2), room(X1, Y1, W1, H1)).

% A room is adjacent to another room if it borders it on the right hand or lower side (or vice versa due to above)
adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), X2 = X1 + W1, Y2 > Y1 - H2, Y2 < Y1 + H1.
adjacent(room(X1, Y1, W1, H1), room(X2, Y2, W2, H2)) :- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2), Y2 = Y1 + H1, X2 > X1 - W2, X2 < X1 + W1.

%* Constraints *%

:- 2 { room_square(X, Y, _, _, _, _)  }, ship(X, Y).
:- corridor(X, Y), corridor(X+1, Y), corridor(X, Y+1), corridor(X+1, Y+1).
:- room(X1, Y1, W1, H1), not reachable(room(X1, Y1, W1, H1)).

%* Preferences *%

#maximize { 1@1,X,Y : corridor(X, Y) }.

% Output predicates.

%#show ship/2.
#show hull/2.
#show room/4.
#show room_square/6.
#show corridor/2.
%#show min_room_X/1.
%#show min_room_Y/1.
%#show min_room/2.
%#show reachable/1.
%#show adjacent/2.