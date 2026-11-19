/*
 * fw_test.h - a tiny unit test harness with no dependencies (host side only).
 *
 * Replace it with Unity, Ceedling, ztest, or the project's own framework when one is
 * in use; the tests below only need ASSERT macros and a way to run them.
 * A failed assertion marks the current test as failed and returns from it.
 */
#ifndef FW_TEST_H
#define FW_TEST_H

#ifdef __cplusplus
extern "C" {
#endif

void fw_test_run(const char *name, void (*fn)(void));
void fw_test_fail(const char *file, int line, const char *message);
void fw_test_fail_int(const char *file, int line, const char *expr, long long expected,
                      long long actual);
int fw_test_summary(void);

#ifdef __cplusplus
}
#endif

#define RUN_TEST(fn) fw_test_run(#fn, (fn))

#define TEST_ASSERT_TRUE(cond)                                   \
    do {                                                         \
        if (!(cond)) {                                           \
            fw_test_fail(__FILE__, __LINE__, "expected true: " #cond); \
            return;                                              \
        }                                                        \
    } while (0)

#define TEST_ASSERT_EQUAL_INT(expected, actual)                                      \
    do {                                                                             \
        long long fw_e_ = (long long)(expected);                                     \
        long long fw_a_ = (long long)(actual);                                       \
        if (fw_e_ != fw_a_) {                                                        \
            fw_test_fail_int(__FILE__, __LINE__, #actual, fw_e_, fw_a_);             \
            return;                                                                  \
        }                                                                            \
    } while (0)

#endif /* FW_TEST_H */
