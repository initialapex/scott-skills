#include "drv_tmp102.h"
#include "fw_test.h"
#include "mock_i2c.h"

/* Build a driver on top of a mock bus; every test starts from this state. */
static void setup(mock_i2c_t *bus, tmp102_t *dev)
{
    tmp102_config_t cfg;

    mock_i2c_init(bus);
    cfg.bus = &bus->base;
    cfg.address = TMP102_ADDR_ADD0_GND;
    cfg.timeout_ms = 0u;
    (void)tmp102_init(dev, &cfg);
}

static void set_raw_register(mock_i2c_t *bus, uint8_t msb, uint8_t lsb)
{
    bus->rx_data[0] = msb;
    bus->rx_data[1] = lsb;
}

static void test_init_rejects_missing_bus(void)
{
    tmp102_t dev;
    tmp102_config_t cfg;

    cfg.bus = NULL;
    cfg.address = TMP102_ADDR_ADD0_GND;
    cfg.timeout_ms = 0u;
    TEST_ASSERT_EQUAL_INT(FW_ERR_INVALID_ARG, tmp102_init(&dev, &cfg));
    TEST_ASSERT_EQUAL_INT(FW_ERR_INVALID_ARG, tmp102_init(NULL, &cfg));
}

static void test_reads_25_degrees(void)
{
    mock_i2c_t bus;
    tmp102_t dev;
    int32_t milli_c = 0;

    setup(&bus, &dev);
    set_raw_register(&bus, 0x19, 0x00); /* 0x190 * 0.0625 C = 25 C */
    TEST_ASSERT_EQUAL_INT(FW_OK, tmp102_read_milli_c(&dev, &milli_c));
    TEST_ASSERT_EQUAL_INT(25000, milli_c);
}

static void test_reads_negative_temperature(void)
{
    mock_i2c_t bus;
    tmp102_t dev;
    int32_t milli_c = 0;

    setup(&bus, &dev);
    set_raw_register(&bus, 0xE7, 0x00); /* two's complement: -25 C */
    TEST_ASSERT_EQUAL_INT(FW_OK, tmp102_read_milli_c(&dev, &milli_c));
    TEST_ASSERT_EQUAL_INT(-25000, milli_c);
}

static void test_reads_largest_value(void)
{
    mock_i2c_t bus;
    tmp102_t dev;
    int32_t milli_c = 0;

    setup(&bus, &dev);
    set_raw_register(&bus, 0x7F, 0xF0); /* 0x7FF * 62.5 mC, truncated toward zero */
    TEST_ASSERT_EQUAL_INT(FW_OK, tmp102_read_milli_c(&dev, &milli_c));
    TEST_ASSERT_EQUAL_INT(127937, milli_c);
}

static void test_sends_register_pointer_to_the_configured_address(void)
{
    mock_i2c_t bus;
    tmp102_t dev;
    int32_t milli_c = 0;

    setup(&bus, &dev);
    (void)tmp102_read_milli_c(&dev, &milli_c);
    TEST_ASSERT_EQUAL_INT(1, bus.call_count);
    TEST_ASSERT_EQUAL_INT(TMP102_ADDR_ADD0_GND, bus.last_addr7);
    TEST_ASSERT_EQUAL_INT(1, bus.last_tx_len);
    TEST_ASSERT_EQUAL_INT(0x00, bus.last_tx[0]);
    TEST_ASSERT_EQUAL_INT(2, bus.last_rx_len);
    TEST_ASSERT_EQUAL_INT(20, bus.last_timeout_ms); /* default timeout */
}

static void test_bus_error_is_returned_and_output_is_untouched(void)
{
    mock_i2c_t bus;
    tmp102_t dev;
    int32_t milli_c = 1234;

    setup(&bus, &dev);
    bus.next_status = FW_ERR_IO;
    TEST_ASSERT_EQUAL_INT(FW_ERR_IO, tmp102_read_milli_c(&dev, &milli_c));
    TEST_ASSERT_EQUAL_INT(1234, milli_c);
}

static void test_reads_through_the_sensor_interface(void)
{
    mock_i2c_t bus;
    tmp102_t dev;
    int32_t milli_c = 0;

    setup(&bus, &dev);
    set_raw_register(&bus, 0x19, 0x00);
    TEST_ASSERT_EQUAL_INT(FW_OK, sensor_read_milli_c(tmp102_as_sensor(&dev), &milli_c));
    TEST_ASSERT_EQUAL_INT(25000, milli_c);
}

void run_tmp102_tests(void)
{
    RUN_TEST(test_init_rejects_missing_bus);
    RUN_TEST(test_reads_25_degrees);
    RUN_TEST(test_reads_negative_temperature);
    RUN_TEST(test_reads_largest_value);
    RUN_TEST(test_sends_register_pointer_to_the_configured_address);
    RUN_TEST(test_bus_error_is_returned_and_output_is_untouched);
    RUN_TEST(test_reads_through_the_sensor_interface);
}
