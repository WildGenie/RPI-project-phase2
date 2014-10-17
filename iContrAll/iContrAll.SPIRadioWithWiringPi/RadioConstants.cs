using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iContrAll.SPIRadio
{
    public static class RadioConstants
    {
        public const byte CMD_RX_FIFO_READ = 0x77;
        public const byte CMD_CTS_READ = 0x44;
        public const byte FIFO_INFO = 0x15;
        public const byte CMD_GET_INT_STATUS = 0x20;
        public const byte CMD_START_RX = 0x32;
        public const byte CMD_POWER_UP = 0x02;
        public const byte CMD_TX_FIFO_WRITE = 0x66;
        public const byte CMD_START_TX = 0x31;


        // CONFIGURATION PARAMETERS
        public const long RADIO_CONFIGURATION_DATA_RADIO_XO_FREQ = 30000000;
        public const byte RADIO_CONFIGURATION_DATA_CHANNEL_NUMBER=0x00;
        public const byte RADIO_CONFIGURATION_DATA_RADIO_PACKET_LENGTH=0x07;
        public const byte RADIO_CONFIGURATION_DATA_RADIO_STATE_AFTER_POWER_UP = 0x03;
        public const int RADIO_CONFIGURATION_DATA_RADIO_DELAY_CNT_AFTER_RESET = 0xF000;


        static byte[] RF_POWER_UP = new byte[] { 0x02, 0x01, 0x00, 0x01, 0xC9, 0xC3, 0x80 };
        static byte[] RF_GPIO_PIN_CFG = new byte[] { 0x13, 0x21, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00 };
        static byte[] RF_GLOBAL_XO_TUNE_1 = new byte[] { 0x11, 0x00, 0x01, 0x00, 0x52 };
        static byte[] RF_GLOBAL_CONFIG_1 = new byte[] { 0x11, 0x00, 0x01, 0x03, 0x60 };
        static byte[] RF_INT_CTL_ENABLE_2 = new byte[] { 0x11, 0x01, 0x02, 0x00, 0x01, 0x30 };
        static byte[] RF_FRR_CTL_A_MODE_4 = new byte[] { 0x11, 0x02, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00 };
        static byte[] RF_PREAMBLE_TX_LENGTH_9 = new byte[] { 0x11, 0x10, 0x09, 0x00, 0x08, 0x14, 0x00, 0x0F, 0x31, 0x00, 0x00, 0x00, 0x00 };
        static byte[] RF_SYNC_CONFIG_5 = new byte[] { 0x11, 0x11, 0x05, 0x00, 0x01, 0xB4, 0x2B, 0x00, 0x00 };
        static byte[] RF_PKT_CRC_CONFIG_1 = new byte[] { 0x11, 0x12, 0x01, 0x00, 0x84 };
        static byte[] RF_PKT_WHT_SEED_15_8_4 = new byte[] { 0x11, 0x12, 0x04, 0x03, 0xFF, 0xFF, 0x00, 0x02 };
        static byte[] RF_PKT_LEN_12 = new byte[] { 0x11, 0x12, 0x0C, 0x08, 0x00, 0x00, 0x00, 0x30, 0x30, 0x00, 0x40, 0x04, 0xAA, 0x00, 0x00, 0x00 };
        static byte[] RF_PKT_FIELD_2_CRC_CONFIG_12 = new byte[] { 0x11, 0x12, 0x0C, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        static byte[] RF_PKT_FIELD_5_CRC_CONFIG_1 = new byte[] { 0x11, 0x12, 0x01, 0x20, 0x00 };
        static byte[] RF_MODEM_MOD_TYPE_12 = new byte[] { 0x11, 0x20, 0x0C, 0x00, 0x02, 0x00, 0x07, 0x01, 0x86, 0xA0, 0x01, 0xC9, 0xC3, 0x80, 0x00, 0x00 };
        static byte[] RF_MODEM_FREQ_DEV_0_1 = new byte[] { 0x11, 0x20, 0x01, 0x0C, 0x57 };
        static byte[] RF_MODEM_TX_RAMP_DELAY_8 = new byte[] { 0x11, 0x20, 0x08, 0x18, 0x01, 0x00, 0x08, 0x03, 0xC0, 0x00, 0x20, 0x10 };
        static byte[] RF_MODEM_BCR_OSR_1_9 = new byte[] { 0x11, 0x20, 0x09, 0x22, 0x00, 0xFA, 0x02, 0x0C, 0x4A, 0x04, 0x19, 0x02, 0x00 };
        static byte[] RF_MODEM_AFC_GEAR_7 = new byte[] { 0x11, 0x20, 0x07, 0x2C, 0x00, 0x12, 0x80, 0x2C, 0x06, 0x9D, 0xE0 };
        static byte[] RF_MODEM_AGC_CONTROL_1 = new byte[] { 0x11, 0x20, 0x01, 0x35, 0xE2 };
        static byte[] RF_MODEM_AGC_WINDOW_SIZE_9 = new byte[] { 0x11, 0x20, 0x09, 0x38, 0x11, 0x37, 0x37, 0x00, 0x02, 0x20, 0x00, 0x00, 0x29 };
        static byte[] RF_MODEM_OOK_CNT1_11 = new byte[] { 0x11, 0x20, 0x0B, 0x42, 0xA4, 0x03, 0xD6, 0x03, 0x00, 0x40, 0x01, 0x80, 0xFF, 0x0C, 0x01 };
        static byte[] RF_MODEM_RSSI_COMP_1 = new byte[] { 0x11, 0x20, 0x01, 0x4E, 0x40 };
        static byte[] RF_MODEM_CLKGEN_BAND_1 = new byte[] { 0x11, 0x20, 0x01, 0x51, 0x08 };
        static byte[] RF_MODEM_CHFLT_RX1_CHFLT_COE13_7_0_12 = new byte[] { 0x11, 0x21, 0x0C, 0x00, 0xFF, 0xBA, 0x0F, 0x51, 0xCF, 0xA9, 0xC9, 0xFC, 0x1B, 0x1E, 0x0F, 0x01 };
        static byte[] RF_MODEM_CHFLT_RX1_CHFLT_COE1_7_0_12 = new byte[] { 0x11, 0x21, 0x0C, 0x0C, 0xFC, 0xFD, 0x15, 0xFF, 0x00, 0x0F, 0xFF, 0xBA, 0x0F, 0x51, 0xCF, 0xA9 };
        static byte[] RF_MODEM_CHFLT_RX2_CHFLT_COE7_7_0_12 = new byte[] { 0x11, 0x21, 0x0C, 0x18, 0xC9, 0xFC, 0x1B, 0x1E, 0x0F, 0x01, 0xFC, 0xFD, 0x15, 0xFF, 0x00, 0x0F };
        static byte[] RF_PA_MODE_4 = new byte[] { 0x11, 0x22, 0x04, 0x00, 0x08, 0x7F, 0x00, 0x3D };
        static byte[] RF_SYNTH_PFDCP_CPFF_7 = new byte[] { 0x11, 0x23, 0x07, 0x00, 0x2C, 0x0E, 0x0B, 0x04, 0x0C, 0x73, 0x03 };
        static byte[] RF_MATCH_VALUE_1_12 = new byte[] { 0x11, 0x30, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        static byte[] RF_FREQ_CONTROL_INTE_8 = new byte[] { 0x11, 0x40, 0x08, 0x00, 0x38, 0x0F, 0xBB, 0xBB, 0x22, 0x22, 0x20, 0xFF };

        public static byte[] RADIO_CONFIGURATION_DATA_ARRAY
        {
            get
            {
                List<byte> r = new List<byte>();
                r.Add(0x07); r.AddRange(RF_POWER_UP);
                r.Add(0x08); r.AddRange(RF_GPIO_PIN_CFG);
                r.Add(0x05); r.AddRange(RF_GLOBAL_XO_TUNE_1);
                r.Add(0x05); r.AddRange(RF_GLOBAL_CONFIG_1);
                r.Add(0x06); r.AddRange(RF_INT_CTL_ENABLE_2);
                r.Add(0x08); r.AddRange(RF_FRR_CTL_A_MODE_4);
                r.Add(0x0D); r.AddRange(RF_PREAMBLE_TX_LENGTH_9);
                r.Add(0x09); r.AddRange(RF_SYNC_CONFIG_5);
                r.Add(0x05); r.AddRange(RF_PKT_CRC_CONFIG_1);
                r.Add(0x08); r.AddRange(RF_PKT_WHT_SEED_15_8_4);
                r.Add(0x10); r.AddRange(RF_PKT_LEN_12);
                r.Add(0x10); r.AddRange(RF_PKT_FIELD_2_CRC_CONFIG_12);
                r.Add(0x05); r.AddRange(RF_PKT_FIELD_5_CRC_CONFIG_1);
                r.Add(0x10); r.AddRange(RF_MODEM_MOD_TYPE_12);
                r.Add(0x05); r.AddRange(RF_MODEM_FREQ_DEV_0_1);
                r.Add(0x0C); r.AddRange(RF_MODEM_TX_RAMP_DELAY_8);
                r.Add(0x0D); r.AddRange(RF_MODEM_BCR_OSR_1_9);
                r.Add(0x0B); r.AddRange(RF_MODEM_AFC_GEAR_7);
                r.Add(0x05); r.AddRange(RF_MODEM_AGC_CONTROL_1);
                r.Add(0x0D); r.AddRange(RF_MODEM_AGC_WINDOW_SIZE_9);
                r.Add(0x0F); r.AddRange(RF_MODEM_OOK_CNT1_11);
                r.Add(0x05); r.AddRange(RF_MODEM_RSSI_COMP_1);
                r.Add(0x05); r.AddRange(RF_MODEM_CLKGEN_BAND_1);
                r.Add(0x10); r.AddRange(RF_MODEM_CHFLT_RX1_CHFLT_COE13_7_0_12);
                r.Add(0x10); r.AddRange(RF_MODEM_CHFLT_RX1_CHFLT_COE1_7_0_12);
                r.Add(0x10); r.AddRange(RF_MODEM_CHFLT_RX2_CHFLT_COE7_7_0_12);
                r.Add(0x08); r.AddRange(RF_PA_MODE_4);
                r.Add(0x0B); r.AddRange(RF_SYNTH_PFDCP_CPFF_7);
                r.Add(0x10); r.AddRange(RF_MATCH_VALUE_1_12);
                r.Add(0x0C); r.AddRange(RF_FREQ_CONTROL_INTE_8);
                r.Add(0x00);

                return r.ToArray();
            }
        }

        public const int D = 10;
        public const int DD = 100;
        public const int FIX_PACKET_LENGTH = 64;
        public const int P = 0;
        public const int TXRX = 1;
        public const int PWDN = 4;
        public const int INT = 6;
    }
}
