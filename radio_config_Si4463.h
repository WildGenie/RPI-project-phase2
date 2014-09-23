/*! @file radio_config.h
 * @brief This file contains the automatically generated
 * configurations.
 *
 * @n WDS GUI Version: 3.2.6.0
 * @n Device: Si4463 Rev.: B1                                 
 *
 * @b COPYRIGHT
 * @n Silicon Laboratories Confidential
 * @n Copyright 2013 Silicon Laboratories, Inc.
 * @n http://www.silabs.com
 */

#ifndef RADIO_CONFIG_H_
#define RADIO_CONFIG_H_

// USER DEFINED PARAMETERS
// Define your own parameters here

// INPUT DATA
/*
// Crys_freq(Hz): 30000000    Crys_tol(ppm): 20    IF_mode: 2    High_perf_Ch_Fil: 1    OSRtune: 0    Ch_Fil_Bw_AFC: 0    ANT_DIV: 0    PM_pattern: 0    
// MOD_type: 2    Rsymb(sps): 10000    Fdev(Hz): 20000    RXBW(Hz): 150000    Manchester: 0    AFC_en: 0    Rsymb_error: 0.0    Chip-Version: 2    
// RF Freq.(MHz): 868    API_TC: 29    fhst: 250000    inputBW: 0    BERT: 0    RAW_dout: 0    D_source: 0    Hi_pfm_div: 1    
// 
// # WB filter 1 (BW = 114.46 kHz);  NB-filter 1 (BW = 114.46 kHz) 
// 
// Modulation index: 4
*/


// CONFIGURATION PARAMETERS
#define RADIO_CONFIGURATION_DATA_RADIO_XO_FREQ                     {30000000L}
#define RADIO_CONFIGURATION_DATA_CHANNEL_NUMBER                    {0x00}
#define RADIO_CONFIGURATION_DATA_RADIO_PACKET_LENGTH               {0x07}
#define RADIO_CONFIGURATION_DATA_RADIO_STATE_AFTER_POWER_UP        {0x03}
#define RADIO_CONFIGURATION_DATA_RADIO_DELAY_CNT_AFTER_RESET       {0xF000}
#define RADIO_CONFIGURATION_DATA_CUSTOM_PAYLOAD					   {0xC5, 0xC5, 0xC5, 0xC5, 0xC5, 0xC5, 0xC5}


// CONFIGURATION COMMANDS


#define RF_POWER_UP 0x02, 0x01, 0x00, 0x01, 0xC9, 0xC3, 0x80
#define RF_GPIO_PIN_CFG 0x13, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
#define RF_GLOBAL_XO_TUNE_1 0x11, 0x00, 0x01, 0x00, 0x52
#define RF_GLOBAL_CONFIG_1 0x11, 0x00, 0x01, 0x03, 0x60
#define RF_INT_CTL_ENABLE_2 0x11, 0x01, 0x02, 0x00, 0x01, 0x38
#define RF_FRR_CTL_A_MODE_4 0x11, 0x02, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00
#define RF_PREAMBLE_TX_LENGTH_9 0x11, 0x10, 0x09, 0x00, 0x08, 0x14, 0x00, 0x0F, 0x31, 0x00, 0x00, 0x00, 0x00
#define RF_SYNC_CONFIG_5 0x11, 0x11, 0x05, 0x00, 0x01, 0xB4, 0x2B, 0x00, 0x00
#define RF_PKT_CRC_CONFIG_1 0x11, 0x12, 0x01, 0x00, 0x80
#define RF_PKT_WHT_SEED_15_8_4 0x11, 0x12, 0x04, 0x03, 0xFF, 0xFF, 0x00, 0x02
#define RF_PKT_LEN_12 0x11, 0x12, 0x0C, 0x08, 0x00, 0x00, 0x00, 0x30, 0x30, 0x00, 0x07, 0x04, 0x00, 0x00, 0x00, 0x00
#define RF_PKT_FIELD_2_CRC_CONFIG_12 0x11, 0x12, 0x0C, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
#define RF_PKT_FIELD_5_CRC_CONFIG_1 0x11, 0x12, 0x01, 0x20, 0x00
#define RF_MODEM_MOD_TYPE_12 0x11, 0x20, 0x0C, 0x00, 0x02, 0x00, 0x07, 0x01, 0x86, 0xA0, 0x01, 0xC9, 0xC3, 0x80, 0x00, 0x02
#define RF_MODEM_FREQ_DEV_0_1 0x11, 0x20, 0x01, 0x0C, 0xBB
#define RF_MODEM_TX_RAMP_DELAY_8 0x11, 0x20, 0x08, 0x18, 0x01, 0x80, 0x08, 0x03, 0xC0, 0x00, 0x20, 0x20
#define RF_MODEM_BCR_OSR_1_9 0x11, 0x20, 0x09, 0x22, 0x01, 0x77, 0x01, 0x5D, 0x86, 0x00, 0xAF, 0x02, 0xC2
#define RF_MODEM_AFC_GEAR_7 0x11, 0x20, 0x07, 0x2C, 0x04, 0x36, 0x80, 0x0F, 0x15, 0x57, 0x80
#define RF_MODEM_AGC_CONTROL_1 0x11, 0x20, 0x01, 0x35, 0xE2
#define RF_MODEM_AGC_WINDOW_SIZE_9 0x11, 0x20, 0x09, 0x38, 0x11, 0x52, 0x52, 0x00, 0x02, 0xFF, 0xFF, 0x00, 0x2A
#define RF_MODEM_OOK_CNT1_11 0x11, 0x20, 0x0B, 0x42, 0xA4, 0x02, 0xD6, 0x83, 0x01, 0x20, 0x01, 0x80, 0xFF, 0x0C, 0x00
#define RF_MODEM_RSSI_COMP_1 0x11, 0x20, 0x01, 0x4E, 0x40
#define RF_MODEM_CLKGEN_BAND_1 0x11, 0x20, 0x01, 0x51, 0x08
#define RF_MODEM_CHFLT_RX1_CHFLT_COE13_7_0_12 0x11, 0x21, 0x0C, 0x00, 0xFF, 0xBA, 0x0F, 0x51, 0xCF, 0xA9, 0xC9, 0xFC, 0x1B, 0x1E, 0x0F, 0x01
#define RF_MODEM_CHFLT_RX1_CHFLT_COE1_7_0_12 0x11, 0x21, 0x0C, 0x0C, 0xFC, 0xFD, 0x15, 0xFF, 0x00, 0x0F, 0xFF, 0xBA, 0x0F, 0x51, 0xCF, 0xA9
#define RF_MODEM_CHFLT_RX2_CHFLT_COE7_7_0_12 0x11, 0x21, 0x0C, 0x18, 0xC9, 0xFC, 0x1B, 0x1E, 0x0F, 0x01, 0xFC, 0xFD, 0x15, 0xFF, 0x00, 0x0F
#define RF_PA_MODE_4 0x11, 0x22, 0x04, 0x00, 0x08, 0x7F, 0x00, 0x3D
#define RF_SYNTH_PFDCP_CPFF_7 0x11, 0x23, 0x07, 0x00, 0x2C, 0x0E, 0x0B, 0x04, 0x0C, 0x73, 0x03
#define RF_MATCH_VALUE_1_12 0x11, 0x30, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
#define RF_FREQ_CONTROL_INTE_8 0x11, 0x40, 0x08, 0x00, 0x38, 0x0E, 0xEE, 0xEE, 0x22, 0x22, 0x20, 0xFF


// AUTOMATICALLY GENERATED CODE! 
// DO NOT EDIT/MODIFY BELOW THIS LINE!
// --------------------------------------------

#ifndef FIRMWARE_LOAD_COMPILE
#define RADIO_CONFIGURATION_DATA_ARRAY { \
        0x07, RF_POWER_UP, \
        0x08, RF_GPIO_PIN_CFG, \
        0x05, RF_GLOBAL_XO_TUNE_1, \
        0x05, RF_GLOBAL_CONFIG_1, \
        0x06, RF_INT_CTL_ENABLE_2, \
        0x08, RF_FRR_CTL_A_MODE_4, \
        0x0D, RF_PREAMBLE_TX_LENGTH_9, \
        0x09, RF_SYNC_CONFIG_5, \
        0x05, RF_PKT_CRC_CONFIG_1, \
        0x08, RF_PKT_WHT_SEED_15_8_4, \
        0x10, RF_PKT_LEN_12, \
        0x10, RF_PKT_FIELD_2_CRC_CONFIG_12, \
        0x05, RF_PKT_FIELD_5_CRC_CONFIG_1, \
        0x10, RF_MODEM_MOD_TYPE_12, \
        0x05, RF_MODEM_FREQ_DEV_0_1, \
        0x0C, RF_MODEM_TX_RAMP_DELAY_8, \
        0x0D, RF_MODEM_BCR_OSR_1_9, \
        0x0B, RF_MODEM_AFC_GEAR_7, \
        0x05, RF_MODEM_AGC_CONTROL_1, \
        0x0D, RF_MODEM_AGC_WINDOW_SIZE_9, \
        0x0F, RF_MODEM_OOK_CNT1_11, \
        0x05, RF_MODEM_RSSI_COMP_1, \
        0x05, RF_MODEM_CLKGEN_BAND_1, \
        0x10, RF_MODEM_CHFLT_RX1_CHFLT_COE13_7_0_12, \
        0x10, RF_MODEM_CHFLT_RX1_CHFLT_COE1_7_0_12, \
        0x10, RF_MODEM_CHFLT_RX2_CHFLT_COE7_7_0_12, \
        0x08, RF_PA_MODE_4, \
        0x0B, RF_SYNTH_PFDCP_CPFF_7, \
        0x10, RF_MATCH_VALUE_1_12, \
        0x0C, RF_FREQ_CONTROL_INTE_8, \
        0x00 \
 }
#else
#define RADIO_CONFIGURATION_DATA_ARRAY { 0 }
#endif

// DEFAULT VALUES FOR CONFIGURATION PARAMETERS
#define RADIO_CONFIGURATION_DATA_RADIO_XO_FREQ_DEFAULT                     30000000L
#define RADIO_CONFIGURATION_DATA_CHANNEL_NUMBER_DEFAULT                    0x00
#define RADIO_CONFIGURATION_DATA_RADIO_PACKET_LENGTH_DEFAULT               0x10
#define RADIO_CONFIGURATION_DATA_RADIO_STATE_AFTER_POWER_UP_DEFAULT        0x01
#define RADIO_CONFIGURATION_DATA_RADIO_DELAY_CNT_AFTER_RESET_DEFAULT       0x1000
#define RADIO_CONFIGURATION_DATA_CUSTOM_PAYLOAD_DEFAULT					   0x42, 0x55, 0x54, 0x54, 0x4F, 0x4E, 0x31 // BUTTON1 

#define RADIO_CONFIGURATION_DATA_RADIO_PATCH_INCLUDED                      0x00
#define RADIO_CONFIGURATION_DATA_RADIO_PATCH_SIZE                          0x00
#define RADIO_CONFIGURATION_DATA_RADIO_PATCH                               {  }

#ifndef RADIO_CONFIGURATION_DATA_ARRAY
#error "This property must be defined!"
#endif

#ifndef RADIO_CONFIGURATION_DATA_RADIO_XO_FREQ
#define RADIO_CONFIGURATION_DATA_RADIO_XO_FREQ         { RADIO_CONFIGURATION_DATA_RADIO_XO_FREQ_DEFAULT }
#endif

#ifndef RADIO_CONFIGURATION_DATA_CHANNEL_NUMBER
#define RADIO_CONFIGURATION_DATA_CHANNEL_NUMBER        { RADIO_CONFIGURATION_DATA_CHANNEL_NUMBER_DEFAULT }
#endif

#ifndef RADIO_CONFIGURATION_DATA_RADIO_PACKET_LENGTH
#define RADIO_CONFIGURATION_DATA_RADIO_PACKET_LENGTH   { RADIO_CONFIGURATION_DATA_RADIO_PACKET_LENGTH_DEFAULT }
#endif

#ifndef RADIO_CONFIGURATION_DATA_RADIO_STATE_AFTER_POWER_UP
#define RADIO_CONFIGURATION_DATA_RADIO_STATE_AFTER_POWER_UP  { RADIO_CONFIGURATION_DATA_RADIO_STATE_AFTER_POWER_UP_DEFAULT }
#endif

#ifndef RADIO_CONFIGURATION_DATA_RADIO_DELAY_CNT_AFTER_RESET
#define RADIO_CONFIGURATION_DATA_RADIO_DELAY_CNT_AFTER_RESET { RADIO_CONFIGURATION_DATA_RADIO_DELAY_CNT_AFTER_RESET_DEFAULT }
#endif

#ifndef RADIO_CONFIGURATION_DATA_CUSTOM_PAYLOAD
#define RADIO_CONFIGURATION_DATA_CUSTOM_PAYLOAD        { RADIO_CONFIGURATION_DATA_CUSTOM_PAYLOAD_DEFAULT }
#endif

#define RADIO_CONFIGURATION_DATA { \
                            Radio_Configuration_Data_Array,                            \
                            RADIO_CONFIGURATION_DATA_CHANNEL_NUMBER,                   \
                            RADIO_CONFIGURATION_DATA_RADIO_PACKET_LENGTH,              \
                            RADIO_CONFIGURATION_DATA_RADIO_STATE_AFTER_POWER_UP,       \
                            RADIO_CONFIGURATION_DATA_RADIO_DELAY_CNT_AFTER_RESET,       \
                            RADIO_CONFIGURATION_DATA_CUSTOM_PAYLOAD                   \
                            }

#endif /* RADIO_CONFIG_H_ */
