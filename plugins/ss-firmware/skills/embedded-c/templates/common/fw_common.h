/*
 * fw_common.h - types and helpers shared by every layer.
 *
 * Depends on the C library only: no vendor header may be included here, so that the
 * file can be compiled on a PC for unit tests.
 */
#ifndef FW_COMMON_H
#define FW_COMMON_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Status codes returned across layer boundaries. Ports translate the vendor codes
 * (esp_err_t, HAL_StatusTypeDef, status_t, negative errno, CyU3PReturnStatus_t) into
 * these, so that upper layers never see a vendor type. Keep FW_OK equal to 0.
 */
typedef enum fw_status {
    FW_OK = 0,
    FW_ERR_INVALID_ARG,   /* a pointer is NULL or a value is out of range            */
    FW_ERR_INVALID_STATE, /* the call is not allowed in the current state            */
    FW_ERR_TIMEOUT,       /* the operation did not finish within its time limit      */
    FW_ERR_BUSY,          /* the resource is in use; try again later                 */
    FW_ERR_IO,            /* the bus or device reported a failure (NACK, CRC, ...)   */
    FW_ERR_NO_MEM,        /* a caller-provided buffer or a pool is too small         */
    FW_ERR_NOT_SUPPORTED, /* the feature or operation is not available               */
    FW_ERR_OVERFLOW       /* data was lost because a queue or buffer was full        */
} fw_status_t;

#define FW_ARRAY_SIZE(a) (sizeof(a) / sizeof((a)[0]))

/* Silence "unused parameter" without changing the signature of an ops callback. */
#define FW_UNUSED(x) ((void)(x))

/*
 * Get the object that contains an embedded member. This is how a method that only
 * receives the base ("interface") pointer finds its concrete object.
 */
#define FW_CONTAINER_OF(ptr, type, member) \
    ((type *)((uint8_t *)(ptr) - offsetof(type, member)))

/*
 * Compile-time assertion that works in C99 (use _Static_assert / static_assert where
 * the toolchain has it). The name must be a unique identifier.
 */
#define FW_STATIC_ASSERT(cond, name) typedef char fw_static_assert_##name[(cond) ? 1 : -1]

/* Big-endian (network order) accessors: use these instead of overlaying a struct. */
static inline uint16_t fw_get_be16(const uint8_t *p)
{
    return (uint16_t)(((uint16_t)p[0] << 8) | p[1]);
}

static inline void fw_put_be16(uint8_t *p, uint16_t v)
{
    p[0] = (uint8_t)(v >> 8);
    p[1] = (uint8_t)(v & 0xFFu);
}

#ifdef __cplusplus
}
#endif

#endif /* FW_COMMON_H */
