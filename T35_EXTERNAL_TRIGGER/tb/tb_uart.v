`timescale 1ns/1ps
module tb_uart;
    localparam CLK_HZ=10000000;
    localparam BAUD=500000;
    localparam BIT_NS=2000;
    reg clk=0, rst_n=0, rx=1;
    reg [7:0] tx_data=0;
    reg tx_valid=0;
    wire [7:0] rx_data;
    wire rx_valid, rx_error;
    wire tx, tx_ready, tx_busy;
    integer i;
    always #50 clk=~clk;
    uart_rx #(.CLK_HZ(CLK_HZ),.BAUD(BAUD)) ur(.clk(clk),.rst_n(rst_n),.rx(rx),.data_out(rx_data),.data_valid(rx_valid),.frame_error(rx_error));
    uart_tx #(.CLK_HZ(CLK_HZ),.BAUD(BAUD)) ut(.clk(clk),.rst_n(rst_n),.data_in(tx_data),.data_valid(tx_valid),.data_ready(tx_ready),.tx(tx),.busy(tx_busy));
    task send_byte;
      input [7:0] v; integer k;
      begin rx=0;#(BIT_NS);for(k=0;k<8;k=k+1)begin rx=v[k];#(BIT_NS);end rx=1;#(BIT_NS);end
    endtask
    initial begin
      #3000;rst_n=1;#3000;send_byte(8'ha5);#3000;
      if(!rx_valid && rx_data!=8'ha5)begin $display("FAIL UART RX data=%02x valid=%b error=%b",rx_data,rx_valid,rx_error);$fatal(1);end
      @(negedge clk);tx_data=8'h3c;tx_valid=1;@(negedge clk);tx_valid=0;
      wait(!tx_busy);#1000;
      $display("PASS tb_uart RX=%02x",rx_data);$finish;
    end
endmodule
