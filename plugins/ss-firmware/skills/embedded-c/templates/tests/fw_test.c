#include "fw_test.h"

#include <stdio.h>

static int s_run;
static int s_failed;
static int s_current_failed;
static const char *s_current_name;

void fw_test_run(const char *name, void (*fn)(void))
{
    s_current_name = name;
    s_current_failed = 0;
    s_run++;
    fn();
    if (s_current_failed != 0) {
        s_failed++;
    } else {
        printf("  PASS %s\n", name);
    }
}

void fw_test_fail(const char *file, int line, const char *message)
{
    s_current_failed = 1;
    printf("  FAIL %s\n       %s:%d: %s\n", s_current_name, file, line, message);
}

void fw_test_fail_int(const char *file, int line, const char *expr, long long expected,
                      long long actual)
{
    s_current_failed = 1;
    printf("  FAIL %s\n       %s:%d: %s expected %lld but was %lld\n", s_current_name, file, line,
           expr, expected, actual);
}

int fw_test_summary(void)
{
    printf("%d tests, %d failed\n", s_run, s_failed);
    return (s_failed == 0) ? 0 : 1;
}
