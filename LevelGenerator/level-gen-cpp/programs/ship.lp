% Specification for the ship, i.e. the overall level shape

width(w).
height(h).

ship(X, Y) :- X = 2..W-1, Y = 2..H-1, width(W), height(H).
hull(X, Y) := X = (1;W), Y = 1..H, width(W), height(H).
hull(X, Y) := X = 1..W, Y = (1;H), width(W), height(H).
