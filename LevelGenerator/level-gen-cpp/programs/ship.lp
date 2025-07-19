% Specification for the ship, i.e. the overall level shape.

grid(1..width, 1..height).

ship(X, Y) :- grid(X, Y), X != 1, X != width, Y != 1, Y != width.
hull(X, Y) :- grid(X, Y), not ship(X, Y).

% Rooms within the ship.

1 { room(XX, YY, W, H) : W=1..3, H=1..3 } 1 :- ship(XX, YY).


% Output predicates.

#show ship/2.
#show hull/2.
#show room/4.
