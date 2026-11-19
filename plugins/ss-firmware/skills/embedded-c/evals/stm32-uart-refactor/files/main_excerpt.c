/* main.c (excerpt) - STM32F411 Nucleo, CubeMX project, USART2 is the ST-Link VCP */
#include "main.h"
#include <string.h>
#include <stdio.h>

extern UART_HandleTypeDef huart2;
extern I2C_HandleTypeDef hi2c1;

uint8_t rx_byte;
char line[64];
int line_len = 0;
volatile int cmd_ready = 0;
float temperature;

/* TMP102 on I2C1, address 0x48 */
float read_temp(void)
{
    uint8_t buf[2];
    HAL_I2C_Mem_Read(&hi2c1, 0x48, 0x00, 1, buf, 2, 100);
    int raw = ((buf[0] << 8) | buf[1]) >> 4;
    return raw * 0.0625f;
}

void HAL_UART_RxCpltCallback(UART_HandleTypeDef *huart)
{
    if (huart->Instance == USART2) {
        if (rx_byte == '\n') {
            line[line_len] = 0;
            cmd_ready = 1;
            line_len = 0;
        } else {
            line[line_len++] = rx_byte;
        }
        HAL_UART_Receive_IT(&huart2, &rx_byte, 1);
    }
}

int main(void)
{
    HAL_Init();
    SystemClock_Config();
    MX_GPIO_Init();
    MX_USART2_UART_Init();
    MX_I2C1_Init();
    /* USER CODE BEGIN 2 */
    HAL_UART_Receive_IT(&huart2, &rx_byte, 1);
    /* USER CODE END 2 */

    while (1) {
        /* USER CODE BEGIN 3 */
        if (cmd_ready) {
            cmd_ready = 0;
            if (strcmp(line, "TEMP?") == 0) {
                char buf[32];
                temperature = read_temp();
                sprintf(buf, "%f\r\n", temperature);
                HAL_UART_Transmit(&huart2, (uint8_t *)buf, strlen(buf), 1000);
            } else if (strcmp(line, "LED ON") == 0) {
                HAL_GPIO_WritePin(LD2_GPIO_Port, LD2_Pin, GPIO_PIN_SET);
            } else if (strcmp(line, "LED OFF") == 0) {
                HAL_GPIO_WritePin(LD2_GPIO_Port, LD2_Pin, GPIO_PIN_RESET);
            } else {
                HAL_UART_Transmit(&huart2, (uint8_t *)"ERR\r\n", 5, 1000);
            }
        }
        /* USER CODE END 3 */
    }
}
