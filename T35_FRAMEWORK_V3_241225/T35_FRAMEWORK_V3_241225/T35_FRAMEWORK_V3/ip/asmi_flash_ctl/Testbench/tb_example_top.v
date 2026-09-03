/////////////////////////////////////////////////////////////////////////////////
//
// EFINIX INC Confidential
// Copyright 2020, Efinix Inc., all rights reserved.
//
// Description:
// Top level testbench for example design in Flash Controller.  
//   
// Date      : 01/07/2019
// Language : Verilog 2001
//
//
// ------------------------------------------------------------------------------
// REVISION:
//  $Snapshot: $
//  $Id:$
/////////////////////////////////////////////////////////////////////////////////

`resetall
`timescale 1ns/1ps     
`include "dbg_defines.v"

module tb_example_top
  ();


   //Parameter
   localparam CLK_PERIOD = 25; //Clock Frequency = 40MHz
  `include "asmi_flash_ctl_define.vh"
                             
     
   //Internal Signals 
   reg rstn;
   reg clkin;
   reg locked;

   wire miso;
   wire [7:0] led;
   wire       sclk;
   wire       nss;
   wire       mosi;
   wire       mosi_out;
   wire       mosi_oe;
   //reg       WPn;
   //reg       HOLDn;
   wire	      WPn;
   wire       HOLDn;
   wire       pll_rstn;
   wire [7:0] dataout;
   wire [7:0] datain;
   wire 	  data_valid;
   wire 	  wren;
   wire		  miso_temp;
   wire		  WPn_temp;
   wire		  HOLDn_temp;

   
   //assign miso_temp = mosi_oe ? miso : 1'bz;
   //assign WPn_temp = mosi_oe ? WPn : 1'bz;
   //assign HOLDn_temp = mosi_oe ? HOLDn : 1'bz;

   //pullup(WPn_temp);
   //pullup(HOLDn_temp);
  
   pullup(WPn);
   pullup(HOLDn);

   assign miso_temp = (mosi_oe) ? miso_temp : miso;
   assign WPn = (mosi_oe) ? WPn_temp : 1'bz;
   assign HOLDn = (mosi_oe) ? HOLDn_temp : 1'bz;

   initial begin
     //$shm_open("test.shm");
     //$shm_probe(tb_example_top,"ACMTF");
     
      $dumpfile("tb_example_top.vcd");
      $dumpvars(0, tb_example_top);
   end
   
   //////////////
   //UUT
   //////////////
      
   example_top
     uut
     (
      // Outputs
      .led				(led[7:0]),
      .led_tr			(),
      .led_ti			(),
      //.led_ti			(),
      .pll_rstn         (pll_rstn), 
      .sclk				(sclk),
      .nss				(nss),
      .mosi_out			(mosi_out),
      .mosi_oe_1		(mosi_oe),
      .dataout		    (dataout),
      .datain           (datain),
      .data_valid       (data_valid),
      .wren			    (wren),
      // Inputs
      .rstn				(rstn),
      .clkin			(clkin),
      .locked			(locked),
      .miso				(miso),
      .miso_1           (mosi)
      );

   
   pullup(mosi);
   assign mosi = (mosi_oe) ? mosi_out : 1'bz;
   
   //////////////////////////
   //External SPI Flash
   //From Winbond
   //////////////////////////

   
   W25Q32JV u_w25q32jv
     (
      // Inouts
      .DIO				(mosi),  //MOSI
      .WPn				(WPn),    
      .HOLDn			(HOLDn), 
      .DO				(miso),  //MISO 
      // Inputs
      .CSn				(nss),   //nss - active low select
      .CLK				(sclk)   //sclk
      );
   
  
   //Intialization
   initial begin
      clkin = 1'b0;
      rstn = 1'b0;
      locked = 1'b1;
      
      
      
      #5000;
      rstn = 1'b1;
     
    end

   always begin
      #(CLK_PERIOD/2);
      clkin = ~clkin;
   end
  
   
   	   initial begin
     	 		$display("///////////////////////////////////////////////////////////////////////");
     	 		$display("//");
     	 		$display("// Test 1: Page Write then read starts running...");
     	 		$display("//");
     	 		$display("///////////////////////////////////////////////////////////////////////");
      
     	 		#210000000;
     	 		$display("Board LED Status : 1 - Green LED ON, 0 - Green LED OFF");
     	 		@(posedge clkin)
     	 		if (led[7:0] == 8'h9F) begin 
     	 			$display("RESULTS(PASS) : LED Display Results : %b", led[7:0]);
     	 		end
     	 		else begin
     	 			$display("Test Failed");
     	 		end
     	 		$finish;
     	 	end
    
      
endmodule //

