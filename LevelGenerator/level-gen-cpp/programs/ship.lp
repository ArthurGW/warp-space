% Specification for the ship, i.e. the overall level shape.

grid(1..width, 1..height).

ship(X, Y) :- grid(X, Y), X != 1, X != width, Y != 1, Y != width.
hull(X, Y) :- grid(X, Y), not ship(X, Y).

% Rooms within the ship.

2 { room(XX, YY, W, H) : ship(XX + W - 1, YY + H - 1), ship(XX, YY) } 4 :- W=2..4, H=2..4.
%8 { room(XX, YY, W, H) : W=2..4, H=2..4, ship(XX + W - 1, YY + H - 1), ship(XX, YY) } 16.
%8 { room(XX, YY, W, H) : W=2..4, H=2..4, ship(XX + W - 1, YY + H - 1), ship(XX, YY) } 16.

room_square(X..(X + W - 1), Y..(Y + H - 1), X, Y, W, H) :- room(X, Y, W, H).
%:- 2 { room_square(X, Y, RX, RY, W, H) }, ship(X, Y), room(RX, RY, W, H).
0 { room_square(X, Y, RX, RY, 2..4, 2..4) : ship(RX, RY) } 1 :- ship(X, Y).

%:- room(X1, Y1, W1, H1), room(X2, Y2, W2, H2).
%, X2 >= X1, X2 < X1 + W1, Y2 >= Y1, Y2 < Y1 + H1.


% Output predicates.

%#show ship/2.
#show hull/2.
#show room/4.
#show room_square/6.
