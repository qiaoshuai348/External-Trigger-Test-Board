`timescale 1ns/1ps

module tb_trigger_capture;
    reg clk = 0;
    reg rst_n = 0;
    reg async_in = 1;
    reg active_low = 1;
    reg monitor_enable = 1;
    reg [31:0] timeout_ticks = 1000;
    reg clear_stats = 0;
    wire [31:0] event_count, last_width, last_period, too_narrow_count;
    wire timeout_flag, overflow_flag, event_pulse;
    integer errors = 0;

    always #5 clk = ~clk;

    trigger_capture dut(
        .clk(clk), .rst_n(rst_n), .async_in(async_in), .active_low(active_low),
        .monitor_enable(monitor_enable), .timeout_ticks(timeout_ticks),
        .clear_stats(clear_stats), .event_count(event_count), .last_width(last_width),
        .last_period(last_period), .too_narrow_count(too_narrow_count),
        .timeout_flag(timeout_flag), .overflow_flag(overflow_flag), .event_pulse(event_pulse)
    );

    task low_pulse;
        input integer duration_ns;
        begin
            #3 async_in = 0;
            #(duration_ns) async_in = 1;
        end
    endtask

    initial begin
        repeat (3) @(negedge clk);
        rst_n = 1;
        repeat (4) @(posedge clk);

        low_pulse(200);
        repeat (8) @(posedge clk);
        if (event_count != 1) begin
            $display("FAIL 200ns event count=%0d", event_count);
            errors = errors + 1;
        end
        if (last_width < 19 || last_width > 21) begin
            $display("FAIL 200ns measured width=%0d", last_width);
            errors = errors + 1;
        end

        repeat (10) @(posedge clk);
        low_pulse(10);
        repeat (8) @(posedge clk);
        if (event_count != 2 || too_narrow_count != 1) begin
            $display("FAIL narrow event count=%0d narrow=%0d", event_count, too_narrow_count);
            errors = errors + 1;
        end
        if (last_period == 0) begin
            $display("FAIL period was not measured");
            errors = errors + 1;
        end

        @(negedge clk); clear_stats = 1;
        @(negedge clk); clear_stats = 0;
        repeat (2) @(posedge clk);
        if (event_count != 0 || last_width != 0 || too_narrow_count != 0) begin
            $display("FAIL clear stats");
            errors = errors + 1;
        end

        if (errors == 0) begin
            $display("PASS tb_trigger_capture");
            $finish;
        end else begin
            $display("FAIL tb_trigger_capture errors=%0d", errors);
            $fatal(1);
        end
    end
endmodule
