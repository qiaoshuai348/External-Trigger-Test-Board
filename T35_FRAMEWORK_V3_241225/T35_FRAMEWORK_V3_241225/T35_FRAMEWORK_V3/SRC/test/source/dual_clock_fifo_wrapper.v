/////////////////////////////////////////////////////////////////////////////
//           _____       
//          / _______    Copyright (C) 2013-2020 Efinix Inc. All rights reserved.
//         / /       \   
//        / /  ..    /   dual_clock_fifo_wrapper.v
//       / / .'     /    
//    __/ /.'      /     Description:
//   __   \       /      Dual Clock FIFO
//  /_/ /\ \_____/ /     
// ____/  \_______/      
//
//********************************
// Revisions:
// 0.0 Initial rev
// 0.1 Added read/write count, almost full, almost empty signal
//********************************

module dual_clock_fifo_wrapper
#(
	parameter	DATA_WIDTH		= 8,
	parameter	ADDR_WIDTH		= 8,
	parameter	LATENCY			= 1,
	parameter	FIFO_MODE		= "STD_FIFO",
	parameter	RAM_INIT_FILE	= "",
	/////////////////////////////////////////////////////////////////////////////////////
	// compatibility	output_reg			check_full_overflow		check_empty_underflow
	/////////////////////////////////////////////////////////////////////////////////////
	// E				user configurable	user configurable		user configurable
	// X				user configurable	always on				always on
	// A				always off			user configurable		user configurable
	parameter	COMPATIBILITY	= "E",
	parameter	OUTPUT_REG		= "TRUE",
	parameter	CHECK_FULL		= "TRUE",
	parameter	CHECK_EMPTY		= "TRUE",
	parameter	AFULL_THRESHOLD	= 512,	
	parameter	AEMPTY_THRESHOLD= 1					
)
(
	input						i_arst,
	
	input						i_wclk,
	input						i_we,
	input	[DATA_WIDTH-1:0]	i_wdata,
	
	input						i_rclk,
	input						i_re,
	
	output						o_full,
	output						o_empty,
	output	[DATA_WIDTH-1:0]	o_rdata,
	
	output						o_afull,	
	output	[ADDR_WIDTH-1:0]	o_wcnt,		
	output						o_aempty,	
	output	[ADDR_WIDTH-1:0]	o_rcnt		
);

generate
	if (COMPATIBILITY == "X")
	begin
		if (FIFO_MODE == "BYPASS")
		begin
			dual_clock_fifo
			#(
				.DATA_WIDTH			(DATA_WIDTH),
				.ADDR_WIDTH			(ADDR_WIDTH),
				.LATENCY			(LATENCY+4),
				.FIFO_MODE			(FIFO_MODE),
				.RAM_INIT_FILE		(RAM_INIT_FILE),
				.COMPATIBILITY		(COMPATIBILITY),
				.OUTPUT_REG			("FALSE"),
				.CHECK_FULL			("TRUE"),
				.CHECK_EMPTY		("TRUE"),
				.AFULL_THRESHOLD	(AFULL_THRESHOLD),
				.AEMPTY_THRESHOLD	(AEMPTY_THRESHOLD)
			)
			inst_dual_clock_fifo
			(
				.i_arst		(i_arst),
				.i_wclk		(i_wclk),
				.i_we		(i_we),
				.i_wdata	(i_wdata),
				.i_rclk		(i_rclk),
				.i_re		(i_re),
				.o_full		(o_full),
				.o_empty	(o_empty),
				.o_rdata	(o_rdata),
				
				.o_afull	(o_afull),
				.o_wcnt		(o_wcnt),
				.o_aempty	(o_aempty),
				.o_rcnt		(o_rcnt)
			);
		end
		else
		begin
			dual_clock_fifo
			#(
				.DATA_WIDTH			(DATA_WIDTH),
				.ADDR_WIDTH			(ADDR_WIDTH),
				.LATENCY			(LATENCY+2),
				.FIFO_MODE			(FIFO_MODE),
				.RAM_INIT_FILE		(RAM_INIT_FILE),
				.COMPATIBILITY		(COMPATIBILITY),
				.OUTPUT_REG			(OUTPUT_REG),
				.CHECK_FULL			("TRUE"),
				.CHECK_EMPTY		("TRUE"),
				.AFULL_THRESHOLD	(AFULL_THRESHOLD),
				.AEMPTY_THRESHOLD	(AEMPTY_THRESHOLD)
			)
			inst_dual_clock_fifo
			(
				.i_arst		(i_arst),
				.i_wclk		(i_wclk),
				.i_we		(i_we),
				.i_wdata	(i_wdata),
				.i_rclk		(i_rclk),
				.i_re		(i_re),
				.o_full		(o_full),
				.o_empty	(o_empty),
				.o_rdata	(o_rdata),
				
				.o_afull	(o_afull),
				.o_wcnt		(o_wcnt),
				.o_aempty	(o_aempty),
				.o_rcnt		(o_rcnt)
			);
		end
	end
	else if (COMPATIBILITY == "A")
	begin
		dual_clock_fifo
		#(
			.DATA_WIDTH			(DATA_WIDTH),
			.ADDR_WIDTH			(ADDR_WIDTH),
			.LATENCY			(LATENCY),
			.FIFO_MODE			(FIFO_MODE),
			.RAM_INIT_FILE		(RAM_INIT_FILE),
			.COMPATIBILITY		(COMPATIBILITY),
			.OUTPUT_REG			("FALSE"),
			.CHECK_FULL			(CHECK_FULL),
			.CHECK_EMPTY		(CHECK_EMPTY),
			.AFULL_THRESHOLD	(AFULL_THRESHOLD),
			.AEMPTY_THRESHOLD	(AEMPTY_THRESHOLD)
		)
		inst_dual_clock_fifo
		(
			.i_arst		(i_arst),
			.i_wclk		(i_wclk),
			.i_we		(i_we),
			.i_wdata	(i_wdata),
			.i_rclk		(i_rclk),
			.i_re		(i_re),
			.o_full		(o_full),
			.o_empty	(o_empty),
			.o_rdata	(o_rdata),
			
			.o_afull	(o_afull),
			.o_wcnt		(o_wcnt),
			.o_aempty	(o_aempty),
			.o_rcnt		(o_rcnt)
		);
	end
	else
	begin
		dual_clock_fifo
		#(
			.DATA_WIDTH			(DATA_WIDTH),
			.ADDR_WIDTH			(ADDR_WIDTH),
			.LATENCY			(LATENCY),
			.FIFO_MODE			(FIFO_MODE),
			.RAM_INIT_FILE		(RAM_INIT_FILE),
			.COMPATIBILITY		(COMPATIBILITY),
			.OUTPUT_REG			(OUTPUT_REG),
			.CHECK_FULL			(CHECK_FULL),
			.CHECK_EMPTY		(CHECK_EMPTY),
			.AFULL_THRESHOLD	(AFULL_THRESHOLD),
			.AEMPTY_THRESHOLD	(AEMPTY_THRESHOLD)
		)
		inst_dual_clock_fifo
		(
			.i_arst		(i_arst),
			.i_wclk		(i_wclk),
			.i_we		(i_we),
			.i_wdata	(i_wdata),
			.i_rclk		(i_rclk),
			.i_re		(i_re),
			.o_full		(o_full),
			.o_empty	(o_empty),
			.o_rdata	(o_rdata),
			
			.o_afull	(o_afull),
			.o_wcnt		(o_wcnt),
			.o_aempty	(o_aempty),
			.o_rcnt		(o_rcnt)
		);
	end
endgenerate

endmodule


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

