# binaire

Binary SRAM PUF analysis tool.

I developed this tool in the course of my master thesis at FH Hagenberg. Basically, it is a C# CLI receiving SRAM PUF data from a microcontroller board (in my case, a Nucleo STM32F401RE). Binaire handles COM port communication with the device over a simple protocol. Also, it handles communication with the database, storing the SRAM PUF data remotely.

For the STM32 source code, see https://github.com/LDrack/stm32-srampuf





--- 

**(C) Embedded Systems Lab / FH Hagenberg**

All rights reserved.


This document contains proprietary information belonging to
Research & Development FH OÖ Forschungs und Entwicklungs GmbH.
Using, passing on and copying of this document or parts of it
is generally not permitted without prior written authorization.

info(at)embedded-lab.at

https://www.embedded-lab.at/


The research on which this source code is based has been partly funded by BMK, BMDW, and the State of Upper Austria in the frame of the COMET Programme managed by FFG.
