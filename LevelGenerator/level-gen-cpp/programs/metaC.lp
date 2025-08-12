% extract atoms in the head of level design rules
level_design(A) :- rule(pos(atom(__level_design(A))),_).
% metasp: minimize over the set {A, not A, concept}
wlist(-1,0,pos(atom(A)),1) :- level_design(A).
wlist(-1,0,neg(atom(A)),1) :- level_design(A).
wlist(-1,0,pos(atom(__concept)),1).
minimize(1,-1).
% metasp: use inclusion−based minimality for that set
optimize(1,1,incl).
% metasp: require that concept is present
:- not hold(atom(__concept)).
