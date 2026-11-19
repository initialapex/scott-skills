#include "mock_i2c.h"

#include <string.h>

static void record(mock_i2c_t *self, uint8_t addr7, const uint8_t *tx, size_t tx_len,
                   size_t rx_len, uint32_t timeout_ms)
{
    self->call_count++;
    self->last_addr7 = addr7;
    self->last_tx_len = tx_len;
    self->last_rx_len = rx_len;
    self->last_timeout_ms = timeout_ms;
    if (tx_len <= MOCK_I2C_BUF_SIZE) {
        memcpy(self->last_tx, tx, tx_len);
    }
}

static fw_status_t mock_write(hal_i2c_t *base, uint8_t addr7, const uint8_t *data, size_t len,
                              uint32_t timeout_ms)
{
    mock_i2c_t *self = FW_CONTAINER_OF(base, mock_i2c_t, base);

    record(self, addr7, data, len, 0u, timeout_ms);
    return self->next_status;
}

static fw_status_t mock_write_read(hal_i2c_t *base, uint8_t addr7, const uint8_t *tx,
                                   size_t tx_len, uint8_t *rx, size_t rx_len,
                                   uint32_t timeout_ms)
{
    mock_i2c_t *self = FW_CONTAINER_OF(base, mock_i2c_t, base);

    record(self, addr7, tx, tx_len, rx_len, timeout_ms);
    if (self->next_status != FW_OK) {
        return self->next_status;
    }
    if (rx_len > MOCK_I2C_BUF_SIZE) {
        return FW_ERR_NO_MEM;
    }
    memcpy(rx, self->rx_data, rx_len);
    return FW_OK;
}

static const hal_i2c_ops_t s_mock_ops = {
    mock_write,
    mock_write_read,
};

void mock_i2c_init(mock_i2c_t *self)
{
    memset(self, 0, sizeof(*self));
    self->base.ops = &s_mock_ops;
    self->next_status = FW_OK;
}
