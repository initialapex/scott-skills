/*
 * drv_tmp102.h - driver for the TI TMP102 I2C temperature sensor (driver layer).
 *
 * Object model:
 *   tmp102_t embeds sensor_t as its first member ("is a" sensor) and holds the bus it was
 *   given ("has a" bus). The caller owns the storage; the driver never allocates.
 *
 * Check the register map and address pins against the datasheet of the part in use.
 */
#ifndef DRV_TMP102_H
#define DRV_TMP102_H

#include "fw_common.h"
#include "hal_i2c.h"
#include "sensor.h"

#ifdef __cplusplus
extern "C" {
#endif

#define TMP102_ADDR_ADD0_GND 0x48u /* 7-bit address with the ADD0 pin tied to ground */

typedef struct tmp102_config {
    hal_i2c_t *bus;      /* required: the bus the sensor sits on                  */
    uint8_t address;     /* 7-bit I2C address                                     */
    uint32_t timeout_ms; /* time limit of one transfer; 0 selects the default     */
} tmp102_config_t;

typedef struct tmp102 {
    sensor_t base; /* MUST be the first member: see tmp102_as_sensor() */
    hal_i2c_t *bus;
    uint8_t address;
    uint32_t timeout_ms;
} tmp102_t;

/* Validate the config and fill the object. Does not touch the bus. */
fw_status_t tmp102_init(tmp102_t *self, const tmp102_config_t *cfg);

/* Read the temperature register and convert it to milli-degrees Celsius. */
fw_status_t tmp102_read_milli_c(tmp102_t *self, int32_t *out_milli_c);

/* View the object through the generic interface (what the service layer receives). */
sensor_t *tmp102_as_sensor(tmp102_t *self);

#ifdef __cplusplus
}
#endif

#endif /* DRV_TMP102_H */
