% Specification for the ship, i.e. the overall level shape.

grid(1..width, 1..height).

ship(X, Y) :- X = 2..width-1, Y = 2..height-1.
hull(X, Y) :- grid(X, Y), not ship(X, Y).

#show ship/2.
#show hull/2.
