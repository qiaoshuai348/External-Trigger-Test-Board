/////////////////////////////////////////////////////////////////////////////
//           _____       
//          / _______    Copyright (C) 2013-2020 Efinix Inc. All rights reserved.
//         / /       \   
//        / /  ..    /   efx_flash_controller_top.v
//       / / .'     /    
//    __/ /.'      /     Description:
//   __   \       /      flash controller top module
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
`include "dbg_defines.v"

  module efx_flash_controller_top
    #(
      parameter ADDR_WIDTH = 24,  //Support 24 or 32
      parameter SCLK_DIV = 2      //Support 2-64
      )  
  (
   //User Logics 
   //Inputs 
   input 		  rst_in, //Active High Reset
   input 		  clk_in, //Input System Clock

   //Command Instruction
   input 		  fast_read, //FAST READ
   input 		  sector_erase, //SECTOR ERASE
   input 		  page_write, //PAGE WRITE
   input 		  fast_read_dual, //FAST READ DUAL 

   //User Control 
   input [ADDR_WIDTH-1:0] address, //24-bits ADDR
   input 		  rden, 
   input 		  wren,
   input 		  shift_bytes, //Shift the data for 
   input [7:0] 		  datain,
   
   output [7:0] 	  dataout,
   output 		  data_valid,
   
   //Status Flags
   output 		  busy, //Busy Status
   
   //Serial Flash Interface
   input 		  miso,    //MISO
   input 		  miso_1,  //MISO Bit-1
   output 		  sclk,    //SCLK
   output 		  nss,     //Chip Select
   output 		  mosi,    //MOSI
   output                 mosi_oe  //Active High Output Enable of MOSI Pin
   );

   //Internal Signals
   wire   fifo_we;
   wire [7:0] fifo_datain;
   wire [7:0] wfifo_data;
   wire       wfifo_rd;
   wire       page_program_done;
   wire       busy_int;

   wire       spi_cmd_en;
   wire [4:0] spi_cmd;
   wire [7:0] spi_wr_data;
   wire [7:0] spi_cmd_instr;
   wire       fast_read_final;
   wire       fast_read_dual_final;
   wire [8:0] byte_cnt;
   wire [7:0] flash_rd_status_reg0;
   
   
// Flash Controller from EFX-A001 Project.
   efx_spi_shifter
     #(
       .SCLK_FREQ((SCLK_DIV-1)),
       .ADDR_WIDTH(ADDR_WIDTH)
       )
     u_efx_spi_shifter
     (
      //Outputs
      .jedec_id_reg                     (),
      .manufacturing_id_reg             (),
      .unique_id_reg                    (),
      .device_id_reg                    (),
      .spi_flash_rd_status_reg0         (flash_rd_status_reg0),
      .spi_flash_rd_status_reg1         (),
      .spi_flash_rd_status_reg2         (),
      .erase_en                         (),
      .fsm_status                       (), 
    
      // Outputs
      .pll_rst				(),                     //active low pll reset. 
      .data_out				(dataout),              //Flash Controller - Return Read Data (Byte) 
      .data_valid			(data_valid),           //Flash Controller - Read Data Valid Flag. 
      .busy				(busy_int),                        
     
      .sclk				(sclk),                 //spi flash SCLK (Port)  
      .nss				(nss),                  //spi active low chip select (Port) 
      .mosi				(mosi),                 //spi MOSI (Port)
      .mosi_oe                          (mosi_oe),              //spi MOSI Output Enable (Active High)
      .wfifo_rd                         (wfifo_rd),             //write fifo (read). 
      .page_program_done                (page_program_done),    // write done flag. 
      
      // Inputs
      .nrst				(~rst_in),               //active low system reset. 
      .clkin				(clk_in),                //2X SCLK Clock 
      .locked				(1'b1),                  //PLL Locked
      .rden                             (rden),             
      .spi_cmd_en                       (spi_cmd_en),         //From efx spi pgm
      .spi_cmd				(spi_cmd[4:0]),       //From efx spi pgm
      .address				(address),            //24 bits ADDRESS FROM USER. 
      .data_in				(spi_wr_data[7:0]),   //From efx spi pgm
      .spi_cmd_instr                    (spi_cmd_instr[7:0]), //From efx spi pgm
      
      .burst_data_wr                    (wfifo_data),
      .miso				(miso),           //spi flash MISO (Port)
      .miso_1                           (miso_1),         //Bit-1 MISO
      .byte_cnt                         (byte_cnt),      
      .w_burst_size                     (8'h01),          //spi burst write data size. 
      .r_burst_size                     (8'h01)           //spi burst read data size. 
      );


//Write FIFO (Store the data)

   function integer depth2width;
      input [31:0] depth;
      begin
	 if (depth > 1) begin
	    depth = depth - 1;
	    for (depth2width=0; depth>0; depth2width = depth2width + 1)
	      depth = depth>>1;
	 end
	 else
	   depth2width = 0;
      end
   endfunction
   
   //FIFO Parameters
   parameter WFIFO_DEPTH = `WFIFO_DEPTH_256 * 256;
   parameter SPI_ADDR_WIDTH = depth2width(WFIFO_DEPTH);
   
			     
 
   assign fifo_we = wren & shift_bytes;
   assign fifo_datain = datain;
   


   dual_clock_fifo_wrapper
     #(
       .DATA_WIDTH(8),                   //Data Width - 8 bits.      
       .ADDR_WIDTH(SPI_ADDR_WIDTH),      //Address - 24 bits.
       .LATENCY(1),                      //Flag latency - 1 
       .FIFO_MODE("STD_FIFO"),           //Standard FIFO
       .RAM_INIT_FILE(""),               //No Intialization file for ram.
       .COMPATIBILITY("E"),              //
       .OUTPUT_REG("FALSE"),             //No output register. 
       .CHECK_FULL("TRUE"),              //Skip the write if fifo full.             
       .CHECK_EMPTY("TRUE"),             //Skip the read if fifo empty. 
       .AFULL_THRESHOLD(WFIFO_DEPTH-1),
       .AEMPTY_THRESHOLD(1)
       )
   u_wfifo
     (
      .i_arst    (rst_in),
      .i_wclk    (clk_in),
      .i_we      (fifo_we),
      .i_wdata   (fifo_datain),
      .i_rclk    (clk_in),          
      .i_re      (wfifo_rd),           //Connect to SPI Shifter. 
      .o_full    (),              
      .o_empty   (),                   //Indicate that the FIFO is empty, ready for software to do burst write. 
      .o_rdata   (wfifo_data),
      .o_afull   (),
      .o_wcnt    (),
      .o_aempty  (),
      .o_rcnt    ()
      );

 

   assign fast_read_final = fast_read & rden;
   assign fast_read_dual_final = fast_read_dual & rden;
   
//Command Controller 
   efx_command_ctl u_efx_command_ctl
     (
      // Outputs
      .byte_cnt_out                     (byte_cnt[8:0]),          //User Byte Counter
      .spi_cmd_en			(spi_cmd_en),             //Command enable 
      .spi_cmd				(spi_cmd[4:0]),           //Command Instruction
      .spi_wr_data			(spi_wr_data[7:0]),       //
      .spi_cmd_instr			(spi_cmd_instr[7:0]),     //Special Command Instruction 
      .busy_out				(busy),                   //This flag indicates :
                                                                  //busy status while executing
                                                                  //multiple instruction command
      // Inputs
      .rst_in				(rst_in),
      .clk_in				(clk_in),
      .wren                             (wren),
      .shift_bytes                      (shift_bytes),      
      .fast_read			(fast_read_final),      //from top - User control
      .fast_read_dual                   (fast_read_dual_final), //from top - User Control
      .sector_erase			(sector_erase),         //from top - User control
      .page_write			(page_write),           //from top - User control
      .flash_rd_status_reg0             (flash_rd_status_reg0), //from efx_spi_shifter block.
      .busy				(busy_int));            //from efx_spi_shifter block to
                                                                //indicate the status of FSM. 
   
   
endmodule // efx_flash_controller_top

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

