/////////////////////////////////////////////////////////////////////////////////
//
// Copyright 2013-2020, Efinix Inc., all rights reserved.
//
// Description:
// Example Top for Flash Controller. 
//
//
// Date       : 23 March 2020
// Department : IP Engineering 
// ------------------------------------------------------------------------------
// REVISION:
//
// 1.0 Initial Release 
/////////////////////////////////////////////////////////////////////////////////
`resetall
`timescale 1 ps/1 ps
`include "dbg_defines.v"

  module example_top
    ( //inputs
      input rstn,       //active low reset 
      input clkin,      //clock from PLL
      input locked,     //locked from PLL
      input miso,       //SPI (flash) : master in , slave out.
      input miso_1, 
      
      //outputs
      output [7:0] dataout,
      output [7:0] datain,
      output data_valid,
      output wren,
      output pll_rstn,  //To reset PLL. 
      output wire [7:0] led,   //8 bit led status.
      output wire [3:0] led_tr,
      output wire [5:0] led_ti,
      output sclk,      //SPI (flash) : Serial Clock
      output nss,       //Chip Select
      output mosi_out,  //SPI (flash) : master out slave in.
      output wire mosi_oe_1
      );

`include "asmi_flash_ctl_define.vh"  
   //Internal Signals
   wire      rst_in;
   wire       busy;
   wire       fast_read;
   wire       sector_erase;
   wire       page_write;
   wire       fast_read_dual;
   wire [ADDR_WIDTH-1:0] address;
   wire 		 rden;
   wire 		 shift_bytes;
   wire          mosi_oe;
   wire [7:0] 		 led_out;
   
   
   //assign miso_1 = 1'bz;
   assign led = led_out;
   assign led_ti[0] = led_out[0];
   assign led_ti[1] = led_out[1];
   assign led_ti[2] = led_out[2];
   assign led_ti[3] = led_out[3];
   assign led_ti[4] = led_out[4];
   assign led_ti[5] = led_out[5];
   
   assign led_tr[0] = ~led_out[0];
   assign led_tr[1] = ~led_out[1];
   assign led_tr[2] = ~led_out[2];
   assign led_tr[3] = ~led_out[3];
   
   assign mosi_oe_1 =  mosi_oe;
   
   assign rst_in = ~rstn;  //Reset all blocks. 
   assign pll_rstn = 1'b1; //Reset the PLL. 
   

       
       
   
   
   asmi_flash_ctl uut(
	 // Outputs
	 .dataout	(dataout[7:0]),
	 .data_valid	(data_valid),
	 .busy		(busy),
	 .sclk		(sclk),
	 .nss		(nss),
	 .mosi		(mosi_out),
     .mosi_1    (),
     .mosi_2    (),
     .mosi_3    (),
	 .mosi_oe	(mosi_oe),
	 // Inputs
	 .rst_in	(rst_in),
	 .clk_in	(clkin),
	 .fast_read	(fast_read),
	 .sector_erase	(sector_erase),
	 .page_write	(page_write),
	 .fast_read_dual(fast_read_dual),
     .quad_enable (),
     .quad_fast_read (),
     .quad_io_fast_read (),
     .quad_page_write (),
	 .address	(address[ADDR_WIDTH-1:0]),
	 .rden		(rden),
	 .wren		(wren),
	 .shift_bytes	(shift_bytes),
	 .datain	(datain[7:0]),
	 .miso		(miso),
	 .miso_1	(miso_1),
     .miso_2    (),
     .miso_3    ());


   flash_test_ctl
      #(.ADDR_WIDTH(ADDR_WIDTH)  //Set the flash to support 24-bits addressing.  
       )
     u_flash_test_ctl
     (
      // Outputs
      .fast_read			(fast_read),
      .sector_erase			(sector_erase),
      .page_write			(page_write),
      .fast_read_dual			(fast_read_dual),
      .address				(address[ADDR_WIDTH-1:0]),
      .rden				(rden),
      .wren				(wren),
      .shift_bytes			(shift_bytes),
      .datain				(datain[7:0]),
      .led_out                          (led_out),
      // Inputs
      .rst_in				(rst_in),
      .clk_in				(clkin),
      .dataout				(dataout[7:0]),
      .data_valid			(data_valid),
      .busy				(busy));
     
  
endmodule // example_top


//////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2013-2019 Efinix Inc. All rights reserved.
//
// This   document  contains  proprietary information  which   is
// protected by  copyright. All rights  are reserved.  This notice
// refers to original work by Efinix, Inc. which may be derivitive
// of other work distributed under license of the authors.  In the
// case of derivative work, nothing in this notice overrides the
// original author's license agreement.  Where applicable, the 
// original license agreement is included in it's original 
// unmodified form immediately below this header.
//
// WARRANTY DISCLAIMER.  
//     THE  DESIGN, CODE, OR INFORMATION ARE PROVIDED “AS IS” AND 
//     EFINIX MAKES NO WARRANTIES, EXPRESS OR IMPLIED WITH 
//     RESPECT THERETO, AND EXPRESSLY DISCLAIMS ANY IMPLIED WARRANTIES, 
//     INCLUDING, WITHOUT LIMITATION, THE IMPLIED WARRANTIES OF 
//     MERCHANTABILITY, NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR 
//     PURPOSE.  SOME STATES DO NOT ALLOW EXCLUSIONS OF AN IMPLIED 
//     WARRANTY, SO THIS DISCLAIMER MAY NOT APPLY TO LICENSEE.
//
// LIMITATION OF LIABILITY.  
//     NOTWITHSTANDING ANYTHING TO THE CONTRARY, EXCEPT FOR BODILY 
//     INJURY, EFINIX SHALL NOT BE LIABLE WITH RESPECT TO ANY SUBJECT 
//     MATTER OF THIS AGREEMENT UNDER TORT, CONTRACT, STRICT LIABILITY 
//     OR ANY OTHER LEGAL OR EQUITABLE THEORY (I) FOR ANY INDIRECT, 
//     SPECIAL, INCIDENTAL, EXEMPLARY OR CONSEQUENTIAL DAMAGES OF ANY 
//     CHARACTER INCLUDING, WITHOUT LIMITATION, DAMAGES FOR LOSS OF 
//     GOODWILL, DATA OR PROFIT, WORK STOPPAGE, OR COMPUTER FAILURE OR 
//     MALFUNCTION, OR IN ANY EVENT (II) FOR ANY AMOUNT IN EXCESS, IN 
//     THE AGGREGATE, OF THE FEE PAID BY LICENSEE TO EFINIX HEREUNDER 
//     (OR, IF THE FEE HAS BEEN WAIVED, $100), EVEN IF EFINIX SHALL HAVE 
//     BEEN INFORMED OF THE POSSIBILITY OF SUCH DAMAGES.  SOME STATES DO 
//     NOT ALLOW THE EXCLUSION OR LIMITATION OF INCIDENTAL OR 
//     CONSEQUENTIAL DAMAGES, SO THIS LIMITATION AND EXCLUSION MAY NOT 
//     APPLY TO LICENSEE.
//
/////////////////////////////////////////////////////////////////////////////

