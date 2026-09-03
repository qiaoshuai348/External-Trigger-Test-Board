`timescale 1ns/1ps

module tb_trigger_generator;
    reg clk = 0;
    reg rst_n = 0;
    reg cfg_apply = 0;
    reg [31:0] cfg_period = 10;
    reg [31:0] cfg_width = 3;
    reg cfg_active_low = 0;
    reg start = 0;
    reg [31:0] start_count = 0;
    reg stop = 0;
    wire trigger_out, running, precharge, pending_update;
    wire [31:0] active_period, active_width, remaining;
    wire active_low, cycle_boundary;
    integer errors = 0;
    integer high_cycles;
    integer low_cycles;

    always #5 clk = ~clk;

    trigger_generator dut(
        .clk(clk), .rst_n(rst_n), .cfg_apply(cfg_apply),
        .cfg_period(cfg_period), .cfg_width(cfg_width),
        .cfg_active_low(cfg_active_low), .start(start),
        .start_count(start_count), .stop(stop), .trigger_out(trigger_out),
        .running(running), .precharge(precharge), .pending_update(pending_update),
        .active_period(active_period), .active_width(active_width),
        .active_low(active_low), .remaining(remaining), .cycle_boundary(cycle_boundary)
    );

    task pulse_cfg;
        begin
            @(negedge clk); cfg_apply = 1;
            @(negedge clk); cfg_apply = 0;
        end
    endtask

    task pulse_start;
        input [31:0] count;
        begin
            @(negedge clk); start_count = count; start = 1;
            @(negedge clk); start = 0;
        end
    endtask

    initial begin
        repeat (3) @(negedge clk);
        rst_n = 1;
        pulse_cfg;
        pulse_start(2);

        high_cycles = trigger_out ? 1 : 0;
        while (running) begin
            @(negedge clk);
            if (running && trigger_out) high_cycles = high_cycles + 1;
        end
        if (high_cycles != 6) begin
            $display("FAIL high-active high cycles=%0d expected=6", high_cycles);
            errors = errors + 1;
        end
        if (trigger_out !== 1'b0 || remaining != 0) begin
            $display("FAIL high-active safe stop");
            errors = errors + 1;
        end

        cfg_active_low = 1;
        pulse_cfg;
        pulse_start(1);
        high_cycles = (running && precharge && trigger_out) ? 1 : 0;
        low_cycles = (running && !precharge && !trigger_out) ? 1 : 0;
        while (running) begin
            @(negedge clk);
            if (running && precharge && trigger_out) high_cycles = high_cycles + 1;
            if (running && !precharge && !trigger_out) low_cycles = low_cycles + 1;
        end
        if (high_cycles != 10) begin
            $display("FAIL low-active precharge=%0d expected=10", high_cycles);
            errors = errors + 1;
        end
        if (low_cycles != 3) begin
            $display("FAIL low-active pulse cycles=%0d expected=3", low_cycles);
            errors = errors + 1;
        end
        if (trigger_out !== 1'b0) begin
            $display("FAIL low-active stopped level is not low");
            errors = errors + 1;
        end

        cfg_active_low = 0;
        cfg_period = 10;
        cfg_width = 3;
        pulse_cfg;
        pulse_start(3);
        repeat (4) @(negedge clk);
        cfg_period = 12;
        cfg_width = 4;
        cfg_apply = 1;
        @(negedge clk); cfg_apply = 0;
        if (!pending_update) begin
            $display("FAIL update was not staged while running");
            errors = errors + 1;
        end
        wait (active_period == 12);
        if (pending_update) begin
            $display("FAIL staged update not committed at boundary");
            errors = errors + 1;
        end
        wait (!running);

        if (errors == 0) begin
            $display("PASS tb_trigger_generator");
            $finish;
        end else begin
            $display("FAIL tb_trigger_generator errors=%0d", errors);
            $fatal(1);
        end
    end
endmodule
