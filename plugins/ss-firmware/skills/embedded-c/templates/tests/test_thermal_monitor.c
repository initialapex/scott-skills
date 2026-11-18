#include "fw_test.h"
#include "svc_thermal_monitor.h"

/* A scripted sensor: implements the same interface as a chip driver. */
typedef struct fake_sensor {
    sensor_t base;
    int32_t value_milli_c;
    fw_status_t status;
} fake_sensor_t;

static fw_status_t fake_sensor_read(sensor_t *base, int32_t *out_milli_c)
{
    fake_sensor_t *self = FW_CONTAINER_OF(base, fake_sensor_t, base);

    if (self->status != FW_OK) {
        return self->status;
    }
    *out_milli_c = self->value_milli_c;
    return FW_OK;
}

static const sensor_ops_t s_fake_sensor_ops = {
    fake_sensor_read,
};

/* Records every event the service reports upward. */
typedef struct event_log {
    unsigned count;
    thermal_event_t last_event;
    int32_t last_value;
} event_log_t;

static void on_event(void *ctx, thermal_event_t event, int32_t value_milli_c)
{
    event_log_t *log = (event_log_t *)ctx;

    log->count++;
    log->last_event = event;
    log->last_value = value_milli_c;
}

typedef struct fixture {
    fake_sensor_t sensor;
    event_log_t log;
    thermal_monitor_t monitor;
} fixture_t;

static void setup(fixture_t *f)
{
    thermal_monitor_config_t cfg;

    f->sensor.base.ops = &s_fake_sensor_ops;
    f->sensor.value_milli_c = 25000;
    f->sensor.status = FW_OK;
    f->log.count = 0u;
    f->log.last_event = THERMAL_EVENT_RECOVERED;
    f->log.last_value = 0;

    cfg.sensor = &f->sensor.base;
    cfg.high_milli_c = 60000;
    cfg.hysteresis_milli_c = 5000;
    cfg.fault_limit = 3u;
    cfg.on_event = on_event;
    cfg.ctx = &f->log;
    (void)thermal_monitor_init(&f->monitor, &cfg);
}

static void poll_with(fixture_t *f, int32_t milli_c)
{
    f->sensor.value_milli_c = milli_c;
    (void)thermal_monitor_poll(&f->monitor);
}

static void test_init_validates_the_config(void)
{
    fixture_t f;
    thermal_monitor_config_t cfg;

    setup(&f);
    cfg = f.monitor.cfg;

    cfg.sensor = NULL;
    TEST_ASSERT_EQUAL_INT(FW_ERR_INVALID_ARG, thermal_monitor_init(&f.monitor, &cfg));
    cfg = f.monitor.cfg;
    cfg.sensor = &f.sensor.base;
    cfg.on_event = NULL;
    TEST_ASSERT_EQUAL_INT(FW_ERR_INVALID_ARG, thermal_monitor_init(&f.monitor, &cfg));
    cfg.on_event = on_event;
    cfg.fault_limit = 0u;
    TEST_ASSERT_EQUAL_INT(FW_ERR_INVALID_ARG, thermal_monitor_init(&f.monitor, &cfg));
    cfg.fault_limit = 3u;
    cfg.hysteresis_milli_c = -1;
    TEST_ASSERT_EQUAL_INT(FW_ERR_INVALID_ARG, thermal_monitor_init(&f.monitor, &cfg));
}

static void test_no_event_below_the_limit(void)
{
    fixture_t f;

    setup(&f);
    poll_with(&f, 59999);
    TEST_ASSERT_EQUAL_INT(0, f.log.count);
    TEST_ASSERT_EQUAL_INT(THERMAL_STATE_NORMAL, thermal_monitor_state(&f.monitor));
}

static void test_over_temp_is_reported_once(void)
{
    fixture_t f;

    setup(&f);
    poll_with(&f, 60000);
    poll_with(&f, 70000);
    TEST_ASSERT_EQUAL_INT(1, f.log.count);
    TEST_ASSERT_EQUAL_INT(THERMAL_EVENT_OVER_TEMP, f.log.last_event);
    TEST_ASSERT_EQUAL_INT(60000, f.log.last_value);
    TEST_ASSERT_EQUAL_INT(THERMAL_STATE_OVER_TEMP, thermal_monitor_state(&f.monitor));
}

static void test_hysteresis_keeps_the_alarm_until_the_clear_level(void)
{
    fixture_t f;

    setup(&f);
    poll_with(&f, 61000);
    poll_with(&f, 55001); /* inside the hysteresis band */
    TEST_ASSERT_EQUAL_INT(1, f.log.count);
    poll_with(&f, 55000); /* high - hysteresis */
    TEST_ASSERT_EQUAL_INT(2, f.log.count);
    TEST_ASSERT_EQUAL_INT(THERMAL_EVENT_RECOVERED, f.log.last_event);
    TEST_ASSERT_EQUAL_INT(THERMAL_STATE_NORMAL, thermal_monitor_state(&f.monitor));
}

static void test_sensor_fault_after_consecutive_failures_only(void)
{
    fixture_t f;

    setup(&f);
    f.sensor.status = FW_ERR_IO;
    (void)thermal_monitor_poll(&f.monitor);
    (void)thermal_monitor_poll(&f.monitor);
    TEST_ASSERT_EQUAL_INT(0, f.log.count);
    TEST_ASSERT_EQUAL_INT(FW_ERR_IO, thermal_monitor_poll(&f.monitor));
    TEST_ASSERT_EQUAL_INT(1, f.log.count);
    TEST_ASSERT_EQUAL_INT(THERMAL_EVENT_SENSOR_FAULT, f.log.last_event);

    (void)thermal_monitor_poll(&f.monitor); /* still failing: not reported again */
    TEST_ASSERT_EQUAL_INT(1, f.log.count);
}

static void test_a_good_read_resets_the_failure_count(void)
{
    fixture_t f;

    setup(&f);
    f.sensor.status = FW_ERR_IO;
    (void)thermal_monitor_poll(&f.monitor);
    (void)thermal_monitor_poll(&f.monitor);
    f.sensor.status = FW_OK;
    poll_with(&f, 25000);
    f.sensor.status = FW_ERR_IO;
    (void)thermal_monitor_poll(&f.monitor);
    (void)thermal_monitor_poll(&f.monitor);
    TEST_ASSERT_EQUAL_INT(0, f.log.count);
}

void run_thermal_monitor_tests(void)
{
    RUN_TEST(test_init_validates_the_config);
    RUN_TEST(test_no_event_below_the_limit);
    RUN_TEST(test_over_temp_is_reported_once);
    RUN_TEST(test_hysteresis_keeps_the_alarm_until_the_clear_level);
    RUN_TEST(test_sensor_fault_after_consecutive_failures_only);
    RUN_TEST(test_a_good_read_resets_the_failure_count);
}
