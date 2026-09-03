/////////////////////////////////////////////////////////////////////////////
//           _____       
//          / _______    Copyright (C) 2013-2020 Efinix Inc. All rights reserved.
//         / /       \   
//        / /  ..    /   efx_command_ctl.v
//       / / .'     /    
//    __/ /.'      /     Description:
//   __   \       /      EFX Command Controller
//  /_/ /\ \_____/ /     
// ____/  \_______/      
//
// *******************************
// Revisions:
// 1.0 Initial rev
//
// *******************************

`resetall
`timescale 1 ps/1 ps

module efx_command_ctl
  (
   //Inputs
   input rst_in,
   input clk_in,
   input wren,
   input shift_bytes, 
   input fast_read,             //Fast Read Control 
   input fast_read_dual,        //Fast Read Dual Control 
   input sector_erase,          //Sector Erase Control
   input page_write,            //Page Write Control
   input busy, 
   input [7:0] flash_rd_status_reg0,
   //Outputs
   output reg [8:0] byte_cnt_out,
   output spi_cmd_en,             
   output [4:0] spi_cmd,
   output [7:0] spi_wr_data,            
   output [7:0] spi_cmd_instr,
   output busy_out                 //This is final busy status, which indicate the instruction busy status. 
   );


//Internal Signals

   reg [3:0] 	current_st, next_st;

   reg 		spi_cmd_en_0;
   reg 		spi_cmd_en_1;
   reg [4:0] 	spi_cmd_0;
   reg [7:0] 	spi_wr_data_0;
   reg [7:0] 	spi_cmd_instr_0;

   reg [7:0] 	spi_wr_data_1;
   reg [7:0] 	spi_cmd_instr_1;
   reg [4:0] 	instr1_1;
   reg [4:0] 	instr2_1;
   reg [4:0] 	instr1_0;
   reg [4:0] 	instr2_0;

   reg 		busy_1;
   reg [4:0] 	spi_cmd_1;
   reg [8:0] 	byte_cnt;
   reg [5:0] 	soft_reset;

   reg 		check_busy_0, check_busy_1;
   

//////////////////////////////////   
//RTL
//////////////////////////////////

   assign busy_out = busy_1;
   
   assign spi_cmd = spi_cmd_1;
   assign spi_cmd_en = spi_cmd_en_1;
   
   assign spi_cmd_instr = spi_cmd_instr_1;
   assign spi_wr_data = spi_wr_data_1;
   
   
/////////////////////////
//Page Write Steps
/////////////////////////
//Write Enable
//Page Program
//Page Done? [Check the status register?]

//Test Case   
//$display("IP Command : Write Enable");
//jtag_intreg_write(`INTREG_IP_CMD,8'h83); //Write Enable Instruction Command. 
//jtag_intreg_write(`INTREG_IP_CMD,8'h00); //Clear Write Enable Instruction Command
//
//$display("HOST START SPI WRITE BURST to SPI FLASH.");
//$display("IP Command : Write");
//jtag_intreg_write(`INTREG_IP_CMD,8'h8c); //Write Instruction Command.
//jtag_intreg_write(`INTREG_IP_CMD,8'h00); //Write Instruction Command.

//Main FSM to handle all command/instructions
   localparam ST_IDLE   = 4'h0;
   localparam ST_CMD0_0 = 4'h1;
   localparam ST_CMD0_1 = 4'h2;
   localparam ST_CMD0_2 = 4'h3;
   localparam ST_CMD0_3 = 4'h4;
   localparam ST_CMD1_0 = 4'h5;
   localparam ST_CMD1_1 = 4'h6;
   localparam ST_CMD1_2 = 4'h7;
   localparam ST_CMD1_3 = 4'h8;
   localparam ST_CMD2_0 = 4'h9;
   localparam ST_CMD2_1 = 4'hA;
   localparam ST_CMD2_2 = 4'hB;
   localparam ST_CMD2_3 = 4'hC;
   localparam ST_CHECK_STATUS0 = 4'hD;
   localparam ST_CHECK_STATUS1 = 4'hE;
   localparam ST_CHECK_STATUS2 = 4'hF;
 
  
always @(posedge clk_in)
  if (rst_in)
    current_st <= ST_IDLE;
  else
    current_st <= next_st;
   
always @(*)
  begin
     next_st = current_st;
     spi_cmd_en_0 = 1'b0;
     spi_cmd_0 = 5'd0;
     spi_wr_data_0 = spi_wr_data_1;
     spi_cmd_instr_0 = spi_cmd_instr_1;

     instr1_0 = instr1_1;
     instr2_0 = instr2_1;

     check_busy_0 = check_busy_1;
     
     case (current_st)
       ST_IDLE: begin
	  //To decide which instruction to kick off.
	  if (sector_erase) begin 
	     next_st = ST_CMD0_0;
	     instr1_0 = 5'h8;
	     instr2_0 = 5'hd;             //Sector Erase
	     check_busy_0 = 1'b1;
	  end
	  
	  else if (fast_read) begin 
	     next_st = ST_CMD1_0;         //Skip WEL
	     instr1_0 = 5'h8;
	     instr2_0 = 5'h14;            //Fast Read
	     check_busy_0 = 1'b0;
	  end

	  else if (fast_read_dual) begin 
	     next_st = ST_CMD1_0;         //Skip WEL
	     instr1_0 = 5'h8;
	     instr2_0 = 5'h15;            //Fast Read Dual
	     check_busy_0 = 1'b0;
	  end
	  
	  else if (page_write) begin 
	     next_st = ST_CMD0_0;
	     instr1_0 = 5'h8;
	     instr2_0 = 5'hc;             //Page Write
	     check_busy_0 = 1'b1; 
	  end

	  else if (soft_reset[5]) begin
	     next_st = ST_CMD1_0;         //Skip WEL
	     instr1_0 = 5'h8;
	     instr2_0 = 5'h1;             //Soft Reset
	     check_busy_0 = 1'b1;
	  end
	  
	   
       end

       ST_CMD0_0: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b1;
	  spi_cmd_0 = instr1_1;         //Write Enable Instruction
	  next_st = ST_CMD0_1;
  
       end

       ST_CMD0_1: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     //Clear the cmd_en
	  spi_cmd_0 = instr1_1;         
	  next_st = ST_CMD0_2;
       end

       ST_CMD0_2: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     
	  spi_cmd_0 = instr1_1;
          if (busy)                //Check for busy status 
	    next_st = ST_CMD0_3;
       end

       ST_CMD0_3 : begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     
	  spi_cmd_0 = instr1_1;
	  if (~busy)
	    next_st = ST_CMD1_0;
       end


       ST_CMD1_0: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b1;
	  spi_cmd_0 = instr2_1;         //Sector Erase Instruction
	  next_st = ST_CMD1_1;
  
       end

       ST_CMD1_1: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     //Clear the cmd_en
	  spi_cmd_0 = instr2_1;         
	  next_st = ST_CMD1_2;
       end

       ST_CMD1_2: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     
	  spi_cmd_0 = instr2_1;
          if (busy)                //Check for busy status 
	    next_st = ST_CMD1_3;
       end

       ST_CMD1_3 : begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     
	  spi_cmd_0 = instr2_1;
	  if (~busy) begin 
	     if (check_busy_1)
	       next_st = ST_CMD2_0;
	     else 
	       next_st = ST_IDLE;
	  end
       end // case: ST_CMD1_3
       
	  
       ST_CMD2_0: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b1;
	  spi_cmd_0 = 5'd14;         //Read Status Register 1
	  next_st = ST_CMD2_1;
       end
       

       ST_CMD2_1: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     //Clear the cmd_en
	  spi_cmd_0 = 5'd14;         
	  next_st = ST_CMD2_2;
       end

       ST_CMD2_2: begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     
	  spi_cmd_0 = 5'd14;
          if (busy)                //Check for busy status 
	    next_st = ST_CMD2_3;
       end

       ST_CMD2_3 : begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     
	  spi_cmd_0 = 5'd14;
	  if (~busy)
	    next_st = ST_CHECK_STATUS0;
       end

       ST_CHECK_STATUS0 : next_st = ST_CHECK_STATUS1;
       ST_CHECK_STATUS1 : next_st = ST_CHECK_STATUS2;
       
       ST_CHECK_STATUS2 : begin
	  spi_cmd_instr_0 = 8'h20;
	  spi_cmd_en_0 = 1'b0;     
	  spi_cmd_0 = 5'd14;
	  if (~flash_rd_status_reg0[0])
	    next_st = ST_IDLE;
	  else
	    next_st = ST_CMD2_0;
       end
      
     endcase // case (current_st)
    
  end // always @ (*)
   

   
   always @(posedge clk_in)
     if (rst_in)
       begin
	  spi_wr_data_1 <= 8'h0;
	  spi_cmd_instr_1 <= 8'd0;
	  spi_cmd_en_1 <= 1'b0;
	  spi_cmd_1 <= 5'b0;
	  instr1_1 <= 5'd0;
	  instr2_1 <= 5'd0;
	  busy_1 <= 1'b0;
	  check_busy_1 <= 1'b0;
       end
     else
       begin
	  spi_wr_data_1 <= spi_wr_data_0;
	  spi_cmd_instr_1 <= spi_cmd_instr_0;
	  spi_cmd_en_1 <= spi_cmd_en_0;
	  spi_cmd_1 <= spi_cmd_0;
	  instr1_1 <= instr1_0;
	  instr2_1 <= instr2_0;
	  busy_1 <= (current_st != ST_IDLE);
	  check_busy_1 <= check_busy_0;
       end // else: !if(rst_in)


   always @(posedge clk_in)
     if (rst_in | (page_write & wren))
       begin
	  byte_cnt <= 9'h000;
       end

     else if (wren & shift_bytes)
       begin
	  byte_cnt <= byte_cnt + 1;
       end

   //Support 1-256
   always @(posedge clk_in)
     if (rst_in)
       byte_cnt_out <= 9'h000;
     else if (page_write & wren)
       byte_cnt_out <= byte_cnt;
 
       
   
   always @(posedge clk_in)
     if (rst_in)
       soft_reset <= 6'b00_0001;
     else
       soft_reset <= soft_reset << 1;
   
/////////////////////////
//Sector Erase Steps
/////////////////////////
//Write Enable Instruction
//Erase
//Erase Done? [Check the status register?]

//Test Case
////Set the Erase Command Instruction to Sector Erase (0x20)
//jtag_intreg_write(`INTREG_IP_INSTR,8'h20); //Erase Instruction Command Setting/Type
//jtag_intreg_read(`INTREG_IP_INSTR,8'h20); //Erase Instruction Command Setting/Type
//
//#100000;
//
////Issue Write Enable Before Any Erase Commands. 
//$display("IP Command : Write Enable");
//jtag_intreg_write(`INTREG_IP_CMD,8'h83); //Write Enable Instruction Command. 
//jtag_intreg_write(`INTREG_IP_CMD,8'h00); //Clear Write Enable Instruction Command
//
//#100000;
//
//$display("IP Command : ERASE");
//jtag_intreg_write(`INTREG_IP_CMD,8'h8d); //Erase Instruction Command.
//jtag_intreg_write(`INTREG_IP_CMD,8'h00); //Erase Instruction Command.

   

//////////////////////////
//Fast Read
//////////////////////////
//Read the data        
//If there's busy status, ignore the fast read instruction. 

//Test Case
//$display("IP Command : Write Enable");
//jtag_intreg_write(`INTREG_IP_CMD,8'h83); //Write Enable Instruction Command. 
//jtag_intreg_write(`INTREG_IP_CMD,8'h00); //Clear Write Enable Instruction Command
//
//#1000;
//
//jtag_intreg_write(`INTREG_IP_CMD,8'h8e); //Burst fast Read Instruction Command.
//jtag_intreg_write(`INTREG_IP_CMD,8'h00); //Burst Read Instruction Command.   
   
   

endmodule // efx_command_ctl



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

