/*
 * hal_i2c.h - I2C master bus interface (HAL layer).
 *
 * This is an "interface" in the object-oriented sense: a base struct that holds a
 * pointer to a const table of operations. A port (port_esp32_i2c.c, port_stm32_i2c.c,
 * ...) embeds hal_i2c_t as the FIRST member of its own struct and fills in the table.
 * Drivers depend on this header only, so they never see a vendor type.
 *
 * Conventions every port must follow:
 *   - addr7 is the 7-bit device address, NOT shifted. A port whose SDK wants the
 *     8-bit form (STM32 HAL) shifts it itself.
 *   - timeout_ms is the time limit of the whole transfer; 0 means "do not wait".
 *   - A bus error returns FW_ERR_IO, a missed deadline FW_ERR_TIMEOUT.
 *   - A bus object serializes its own transfers (one mutex inside the port), so
 *     callers need no locking of their own.
 */
#ifndef HAL_I2C_H
#define HAL_I2C_H

#include "fw_common.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct hal_i2c hal_i2c_t;

typedef struct hal_i2c_ops {
    /* START, address+W, data, STOP. */
    fw_status_t (*write)(hal_i2c_t *self, uint8_t addr7, const uint8_t *data, size_t len,
                         uint32_t timeout_ms);
    /* START, address+W, tx, repeated START, address+R, rx, STOP. */
    fw_status_t (*write_read)(hal_i2c_t *self, uint8_t addr7, const uint8_t *tx, size_t tx_len,
                              uint8_t *rx, size_t rx_len, uint32_t timeout_ms);
} hal_i2c_ops_t;

struct hal_i2c {
    const hal_i2c_ops_t *ops;
};

/* Thin wrappers: check the arguments once, so that every caller does not have to. */
static inline fw_status_t hal_i2c_write(hal_i2c_t *bus, uint8_t addr7, const uint8_t *data,
                                        size_t len, uint32_t timeout_ms)
{
    if (bus == NULL || bus->ops == NULL || bus->ops->write == NULL || data == NULL) {
        return FW_ERR_INVALID_ARG;
    }
    return bus->ops->write(bus, addr7, data, len, timeout_ms);
}

static inline fw_status_t hal_i2c_write_read(hal_i2c_t *bus, uint8_t addr7, const uint8_t *tx,
                                             size_t tx_len, uint8_t *rx, size_t rx_len,
                                             uint32_t timeout_ms)
{
    if (bus == NULL || bus->ops == NULL || bus->ops->write_read == NULL || tx == NULL ||
        rx == NULL) {
        return FW_ERR_INVALID_ARG;
    }
    return bus->ops->write_read(bus, addr7, tx, tx_len, rx, rx_len, timeout_ms);
}

#ifdef __cplusplus
}
#endif

#endif /* HAL_I2C_H */
