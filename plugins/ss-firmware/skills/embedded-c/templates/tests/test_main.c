#include <stdio.h>

#include "fw_test.h"

void run_tmp102_tests(void);
void run_thermal_monitor_tests(void);

int main(void)
{
    printf("tmp102 driver\n");
    run_tmp102_tests();
    printf("thermal monitor service\n");
    run_thermal_monitor_tests();
    return fw_test_summary();
}
