#include "drv_tmp102.h"

#define TMP102_REG_TEMPERATURE 0x00u
#define TMP102_DEFAULT_TIMEOUT_MS 20u

/* The temperature is a 12-bit two's complement value, left-justified in 16 bits. */
#define TMP102_RAW_SHIFT 4u
#define TMP102_RAW_SIGN_BIT 0x0800
#define TMP102_RAW_RANGE 0x1000
/* One LSB is 0.0625 C = 62.5 milli-degrees = 625 / 10. */
#define TMP102_MILLI_C_NUM 625
#define TMP102_MILLI_C_DEN 10

/* Method of the sensor interface: recover the concrete object from the base pointer. */
static fw_status_t tmp102_sensor_read(sensor_t *base, int32_t *out_milli_c)
{
    tmp102_t *self = FW_CONTAINER_OF(base, tmp102_t, base);

    return tmp102_read_milli_c(self, out_milli_c);
}

/* One table for all instances; const, so it lives in flash and costs no RAM. */
static const sensor_ops_t s_tmp102_sensor_ops = {
    tmp102_sensor_read,
};

fw_status_t tmp102_init(tmp102_t *self, const tmp102_config_t *cfg)
{
    if (self == NULL || cfg == NULL || cfg->bus == NULL) {
        return FW_ERR_INVALID_ARG;
    }

    self->base.ops = &s_tmp102_sensor_ops;
    self->bus = cfg->bus;
    self->address = cfg->address;
    self->timeout_ms = (cfg->timeout_ms != 0u) ? cfg->timeout_ms : TMP102_DEFAULT_TIMEOUT_MS;
    return FW_OK;
}

fw_status_t tmp102_read_milli_c(tmp102_t *self, int32_t *out_milli_c)
{
    static const uint8_t reg = TMP102_REG_TEMPERATURE;
    uint8_t rx[2];
    int32_t raw;
    fw_status_t st;

    if (self == NULL || self->bus == NULL || out_milli_c == NULL) {
        return FW_ERR_INVALID_ARG;
    }

    st = hal_i2c_write_read(self->bus, self->address, &reg, sizeof(reg), rx, sizeof(rx),
                            self->timeout_ms);
    if (st != FW_OK) {
        return st;
    }

    raw = (int32_t)(fw_get_be16(rx) >> TMP102_RAW_SHIFT);
    if ((raw & TMP102_RAW_SIGN_BIT) != 0) {
        raw -= TMP102_RAW_RANGE;
    }
    *out_milli_c = (raw * TMP102_MILLI_C_NUM) / TMP102_MILLI_C_DEN;
    return FW_OK;
}

sensor_t *tmp102_as_sensor(tmp102_t *self)
{
    return (self != NULL) ? &self->base : NULL;
}
