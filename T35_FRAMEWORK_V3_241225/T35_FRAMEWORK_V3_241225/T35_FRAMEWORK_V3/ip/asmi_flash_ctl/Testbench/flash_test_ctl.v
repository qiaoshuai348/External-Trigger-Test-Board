/////////////////////////////////////////////////////////////////////////////////
//
// Copyright 2013-2020, Efinix Inc., all rights reserved.
//
// Description:
// Flash Test Controller
// Issue page write and read command to flash controller.
//
//
// Date : 24 March 2020
// Department : IP Engineering 
// ------------------------------------------------------------------------------
// REVISION:
//
// 1.0 Initial Release 
/////////////////////////////////////////////////////////////////////////////////
`resetall
`timescale 1 ps/1 ps
`include "dbg_defines.v"

  module flash_test_ctl
    #(parameter ADDR_WIDTH = 24)
    (
    

     //inputs
     input rst_in,
     input clk_in,
     input [7:0] dataout,
     input data_valid,
     input busy,
     
     //outputs
     output reg fast_read,
     output reg sector_erase,
     output reg page_write,
     output reg fast_read_dual,
     output wire [ADDR_WIDTH-1:0] address,
     output reg rden,
     output reg wren,
     output reg shift_bytes,
     output reg [7:0] datain,
     output wire [7:0] led_out
     );

   
   //assign fast_read_dual = 1'b0;
   assign address = 24'h00_0C35_0000;
   
   //Signals
   reg [8:0]  current_st, next_st;
   reg 	      count_en;
   reg [127:0] count;
   reg 	      wren_reg;
   reg 	      shift_bytes_reg;
   reg [7:0]  datain_reg, led_out_reg;
   reg 	      rden_reg;
   reg 	      fast_read_reg;
   reg        fast_read_dual_reg;
   reg 	      page_write_reg;
   reg 	      sector_erase_reg;
   reg        quad_enable_reg;
   reg        quad_fast_read_reg;
   reg        quad_page_write_reg;
   reg        erase_a; //0 - normal write 1- page write
   reg		  data_valid_reg;

   localparam IDLE_ST = 8'h00,
     WAIT_ST = 8'h01,
     WRITE0_ST = 8'h02,
     WRITE1_ST = 8'h03,
     WRITE2_ST = 8'h04,
     WRITE3_ST = 8'h05,
     READ0_ST = 8'h06,
     READ1_ST = 8'h07,
     ERASE0_ST = 8'h08,
     ERASE1_ST = 8'h09,
     ERASE2_ST = 8'h0A,
     WAIT2_ST = 8'h0B,
     DONE_ST = 8'h0C,
     WAIT1_ST = 8'h0D, 
     READDUAL0_ST = 8'h0E,
     READDUAL1_ST = 8'h0F,
     WAIT3_ST     = 8'h10,
     QUADEN_ST = 8'h11,
     QUADWRITE0_ST = 8'h12,
     QUADWRITE1_ST = 8'h13,
     QUADWRITE2_ST = 8'h14,
     QUADWRITE3_ST = 8'h15,
     QUADREAD0_ST = 8'h16,
     QUADREAD1_ST = 8'h17,
     QUADREADIO0_ST = 8'h18,
     QUADREADIO1_ST = 8'h19,
     WAIT4_ST = 8'h2A,
     WAIT5_ST = 8'h2B,
     WAIT6_ST = 8'h2C,
     WAITQE_ST = 8'h2D;
   
   
          
     
     
   always @(posedge clk_in)
     if (rst_in)
       current_st <= IDLE_ST;
     else
       current_st <= next_st;
   
            always @*
             begin
            //defaults 
            next_st = current_st;
            count_en = 1'b0;
            wren_reg = 1'b0;
            page_write_reg = 1'b0;
            shift_bytes_reg = 1'b0;
            datain_reg = 8'h00;
            rden_reg = 1'b0;
            fast_read_reg = 1'b0;
            fast_read_dual_reg = 1'b0;
            sector_erase_reg  = 1'b0;
            //quad_io_fast_read_reg = 1'b0;
            quad_page_write_reg = 1'b0;
            quad_enable_reg = 1'b0;
            data_valid_reg = 1'b0;
            case (current_st)
              IDLE_ST: begin
                 if (~busy)
                 next_st = WAIT_ST;	     
              end
        
              WAIT_ST: begin
                 count_en = 1'b1;
        
                 if (count == `WAIT_TIME)
                   next_st = ERASE0_ST ;
              end
              
              ERASE0_ST : begin
                 wren_reg = 1'b1;
                 sector_erase_reg = 1'b1;
                 next_st = ERASE1_ST;
             end
             
               ERASE1_ST : begin
                    if (busy)
                     next_st = ERASE2_ST;
               end  
               
               ERASE2_ST : begin
                   if (~busy)
                     next_st = WAIT2_ST;
               end
               
              WAIT2_ST: begin
                 count_en = 1'b1;
        
                 if (count == `WAIT_TIME)
                   next_st = WRITE0_ST;
              end
        
              WRITE0_ST : begin
                 wren_reg = 1'b1;
                 shift_bytes_reg = 1'b1;
                 datain_reg = 8'h9F;
        
                 next_st = WRITE1_ST;
                 
              end
        
              WRITE1_ST : begin
                 wren_reg = 1'b1;
                 page_write_reg = 1'b1;
        
                 next_st = WRITE2_ST;
                 
              end
        
              WRITE2_ST : begin
                 if (busy)
                   next_st = WRITE3_ST;
              end
        
              WRITE3_ST: begin
                 if (~busy)
                   next_st = WAIT1_ST;
              end
              
              
              WAIT1_ST: begin
                 count_en = 1'b1;
        
                 if (count == `WAIT_TIME)
                   next_st = READ0_ST;
              end
              
        
              
              READ0_ST: begin
                 rden_reg = 1'b1;
                 fast_read_reg = 1'b1;
                 next_st = READ1_ST;
              end
        
              READ1_ST: begin
                 rden_reg = 1'b1;
                 if (data_valid) begin
                   data_valid_reg = 1'b1;
                   next_st = WAIT3_ST;		
                 end
                 
              end
              
              WAIT3_ST: begin
                count_en = 1'b1;
                 if (count == `WAIT_TIME)  
                   next_st =  READDUAL0_ST;
              end
              
              READDUAL0_ST: begin
                rden_reg = 1'b1;                    
                fast_read_dual_reg = 1'b1;
                next_st = READDUAL1_ST;
              end
              
              READDUAL1_ST: begin
                rden_reg = 1'b1;
                 if (data_valid) begin
                     data_valid_reg = 1'b1;
                     next_st = DONE_ST;
                 end
              end
        
              DONE_ST : begin
                 next_st = DONE_ST;
              end
             
              endcase // case (current_st) 
            end

   always @(posedge clk_in)
     begin 
	if (rst_in)
	  count <= 128'h0;
	else if (count_en)
	  count <= count + 1;
	else 
	  count <= 128'h0;
     end
   
   always @(posedge clk_in)
     begin
	if (rst_in)
	  begin
	     wren <= 1'b0;
	     shift_bytes <= 1'b0;
	     datain <= 8'h00;
	     rden <= 1'b0;
	     fast_read <= 1'b0;
         fast_read_dual <= 1'b0;
	     page_write <= 1'b0;
	     sector_erase <= 1'b0;
	     led_out_reg <= 8'hff;
	  end

	else
	  begin
	     wren <= wren_reg;
	     shift_bytes <= shift_bytes_reg;
	     datain <= datain_reg;
	     rden <= rden_reg;
	     fast_read <= fast_read_reg;
         fast_read_dual <= fast_read_dual_reg;
	     page_write <= page_write_reg;
	     sector_erase <= sector_erase_reg;
         if (data_valid_reg)  
            led_out_reg <= dataout;
	  end 
     end
     
     
     assign led_out = led_out_reg[7:0];
   
       
   
endmodule // flash_test_ctl

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

