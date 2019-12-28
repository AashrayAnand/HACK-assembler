@2
M=0
  @R1
  M=M-1 // R1 > 0 at start, so we can decrement at least once
  D=M+1
  @out // set jump point
  D;JEQ // jump to OUT loop if R1 = 0
  @R0
  D=M // store R0
  @R2
  M=M+D // increase R2 by R0
  @2 // jump back into loop
  0;JEQ
(out)
  @out // set jump point
  0;JEQ
