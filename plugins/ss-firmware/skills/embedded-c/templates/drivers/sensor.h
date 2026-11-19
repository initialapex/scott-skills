/*
 * sensor.h - generic temperature sensor interface (driver layer).
 *
 * The service layer talks to this interface, not to a chip. A TMP102, an NTC on an ADC,
 * or a test double each implement it, so the service never changes when the part does.
 */
#ifndef SENSOR_H
#define SENSOR_H

#include "fw_common.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct sensor sensor_t;

typedef struct sensor_ops {
    /* Read one temperature sample in milli-degrees Celsius (25.5 C = 25500). */
    fw_status_t (*read_milli_c)(sensor_t *self, int32_t *out_milli_c);
} sensor_ops_t;

struct sensor {
    const sensor_ops_t *ops;
};

static inline fw_status_t sensor_read_milli_c(sensor_t *self, int32_t *out_milli_c)
{
    if (self == NULL || self->ops == NULL || self->ops->read_milli_c == NULL ||
        out_milli_c == NULL) {
        return FW_ERR_INVALID_ARG;
    }
    return self->ops->read_milli_c(self, out_milli_c);
}

#ifdef __cplusplus
}
#endif

#endif /* SENSOR_H */
