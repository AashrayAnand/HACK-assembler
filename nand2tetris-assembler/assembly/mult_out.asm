R2 -> 0000000000000010
M==>0
R1 -> 0000000000000001
M==>M-1
D==>M+1
out -> 0000000000001101
D--->JEQ
R0 -> 0000000000000000
D==>M
R2 -> 0000000000000010
M==>M+D
loop -> 0000000000000010
0--->JEQ
out -> 0000000000001101
0--->JEQ
