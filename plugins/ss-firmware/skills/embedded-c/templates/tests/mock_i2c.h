/*
 * mock_i2c.h - a test double for hal_i2c_t.
 *
 * It implements the same interface as a real port, records what the driver sent, and
 * returns scripted data or a scripted error. This is why drivers take an interface
 * instead of calling the vendor HAL: the driver runs unchanged on a PC.
 */
#ifndef MOCK_I2C_H
#define MOCK_I2C_H

#include "hal_i2c.h"

#define MOCK_I2C_BUF_SIZE 16u

typedef struct mock_i2c {
    hal_i2c_t base; /* first member, like a real port */

    /* Script: what the next transfer returns. */
    fw_status_t next_status;
    uint8_t rx_data[MOCK_I2C_BUF_SIZE];

    /* Record: what the driver asked for. */
    unsigned call_count;
    uint8_t last_addr7;
    uint8_t last_tx[MOCK_I2C_BUF_SIZE];
    size_t last_tx_len;
    size_t last_rx_len;
    uint32_t last_timeout_ms;
} mock_i2c_t;

void mock_i2c_init(mock_i2c_t *self);

#endif /* MOCK_I2C_H */
