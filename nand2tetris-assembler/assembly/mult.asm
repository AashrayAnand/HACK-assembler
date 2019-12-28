// R1, with the JMP condition being that R1 = 0, meaning we have added
// R0 to sum R1 times
@R2
M=0

(loop)
  @R1
  M=M-1 // R1 > 0 at start, so we can decrement at least once
  D=M+1
  @out // set jump point
  D;JEQ // jump to OUT loop if R1 = 0


  @R0
  D=M // store R0
  @R2
  M=M+D // increase R2 by R0

  @loop // jump back into loop
  0;JEQ

(out)
  @out // set jump point
  0;JEQ
