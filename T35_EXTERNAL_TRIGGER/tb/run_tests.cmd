@echo off
setlocal
set IVL=C:\iverilog\bin\iverilog.exe
set VVP=C:\iverilog\bin\vvp.exe
if not exist "%IVL%" (
  echo ERROR: Icarus Verilog not found at %IVL%
  exit /b 2
)

if not exist build mkdir build

"%IVL%" -g2005 -Wall -s tb_trigger_generator -o build\tb_trigger_generator.vvp ..\rtl\trigger_generator.v tb_trigger_generator.v
if errorlevel 1 exit /b 1
"%VVP%" build\tb_trigger_generator.vvp
if errorlevel 1 exit /b 1

"%IVL%" -g2005 -Wall -s tb_trigger_capture -o build\tb_trigger_capture.vvp ..\rtl\trigger_capture.v tb_trigger_capture.v
if errorlevel 1 exit /b 1
"%VVP%" build\tb_trigger_capture.vvp
if errorlevel 1 exit /b 1

"%IVL%" -g2005 -Wall -s tb_uart -o build\tb_uart.vvp ..\rtl\uart_tx.v ..\rtl\uart_rx.v tb_uart.v
if errorlevel 1 exit /b 1
"%VVP%" build\tb_uart.vvp
if errorlevel 1 exit /b 1

"%IVL%" -g2005 -Wall -s tb_protocol -o build\tb_protocol.vvp ..\rtl\uart_tx.v ..\rtl\uart_rx.v ..\rtl\trigger_generator.v ..\rtl\trigger_capture.v ..\rtl\external_trigger_controller.v tb_protocol.v
if errorlevel 1 exit /b 1
"%VVP%" build\tb_protocol.vvp
if errorlevel 1 exit /b 1

echo ALL RTL TESTS PASSED
exit /b 0
