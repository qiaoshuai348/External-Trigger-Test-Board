/////////////////////////////////////////////////////////////////////////////////
//
// Copyright (C) 2013-2019 Efinix Inc. All rights reserved.
//
// Description:
// 
// Efinix soft logic DDR system reset controller -- Simulation Testbench
//
// This testbench demonstrates typical usage of DDR soft logic reset controller
//
// Language  : Verilog 2001
//
//
// ------------------------------------------------------------------------------
// REVISION:
//  $Snapshot: $
//  $Id:$
//
// History:
// 1.0 Initial Release. 
/////////////////////////////////////////////////////////////////////////////////
`resetall
`timescale 1ns / 1ps


module sim();

	reg user_reset_n;
	reg clock;
	integer cycle_count;
	
	`include "ddr_reset_control_define.vh"
	
	/* dangling wires here... if DDR were instantiated, we'd connect
	these wires to the DDR's reset interface */
	wire ddr_rstn;
	wire ddr_cfg_seq_rst;
	wire ddr_cfg_seq_start;
	
	/* monitor done status */
	wire ddr_init_done;

	// Initialize inputs
	initial begin
		$dumpfile("ddr_reset_sequencer_waveform.vcd");
		$dumpvars(0, user_reset_n, clock, ddr_rstn, ddr_cfg_seq_rst, ddr_cfg_seq_start, ddr_init_done);
		// $shm_open("test_jig.shm");
		// $shm_probe(sim,"ACMTF");
	
		user_reset_n <= 1'b1;
		clock <= 1'b0;
		cycle_count <= 0;
	end

	// Generate the clock, 50 MHz
	always #10 clock = ~clock;

	// stimulus
	always @(negedge clock) begin
		if (cycle_count == 10) begin
			// assert reset
			user_reset_n <= 1'b0;
		end else if (cycle_count == 20) begin
			// release reset, begin init
			user_reset_n <= 1'b1;
		end
		cycle_count <= cycle_count + 1;
	end
	
	// check done status
	always @(posedge clock) begin
		//$display("** time: %0t, rstn: %b, seqrst: %b, seqstart: %b, done: %b", $time, ddr_rstn, ddr_cfg_seq_rst, ddr_cfg_seq_start, ddr_init_done);
		if ((cycle_count > 20) && (cycle_count < (FREQ * 1000))) begin
			if (ddr_init_done == 1'b1) begin
				// init not expected to be finished yet
				$display("Did not expect initialization to be finished");
				$display("TEST : FAIL");
				$finish();
			end
		end else if (cycle_count == 180000) begin
			// init expected to be finished
			if (ddr_init_done == 1'b0) begin
				$display("Expected initialization to be finished");
				$display("TEST : FAIL");
			end else begin
				$display("TEST : PASS");
			end
			$finish();
		end
	end


	// unit under test 
        ddr_reset_control dut_ddr_reset_sequencer
	(
		.ddr_rstn_i(user_reset_n),
		.clk(clock),
		
		.ddr_rstn(ddr_rstn),
		.ddr_cfg_seq_rst(ddr_cfg_seq_rst),
		.ddr_cfg_seq_start(ddr_cfg_seq_start),
		
		.ddr_init_done(ddr_init_done)
	);

endmodule 
