onerror {quit -f}
vlib work
vlog +define+SIM+SIM_MODE+EFX_SIM -sv ./tb_example_top.v
vlog +define+SIM+SIM_MODE+EFX_SIM -sv ./dbg_defines.v
vlog +define+SIM+SIM_MODE+EFX_SIM -sv ./W25Q16JV.v
vlog +define+SIM+SIM_MODE+EFX_SIM -sv ./W25Q32JV.v
vlog +define+SIM+SIM_MODE+EFX_SIM -sv ./W25Q256JVxIQ.v
vlog +define+SIM+SIM_MODE+EFX_SIM -sv ./example_top.v
vlog +define+SIM+SIM_MODE+EFX_SIM -sv ./flash_test_ctl.v
vlog +define+SIM+SIM_MODE+EFX_SIM -sv ./asmi_flash_ctl.v
vsim -t ns work.tb_example_top
run -all
