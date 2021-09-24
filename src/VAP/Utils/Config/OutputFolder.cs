// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

 namespace Utils.Config
{
    public static class OutputFolder
    {
        public static string OutputFolderAll { get; set; } = "output_all/";

        public static string OutputFolderBGSLine { get; set; } = "output_bgsline/";

        public static string OutputFolderLtDNN { get; set; } = "output_ltdnn/";

        public static string OutputFolderCcDNN { get; set; } = "output_ccdnn/";

        public static string OutputFolderAML { get; set; } = "output_aml/";

        public static string OutputFolderFrameDNNDarknet { get; set; } = "output_framednndarknet/";

        public static string OutputFolderFrameDNNTF { get; set; } = "output_framednntf/";

        public static string OutputFolderFrameDNNONNX { get; set; } = "output_framednnonnx/";
        public static string OutputFolderMaskRCNNONNX { get; set; } = "output_maskrcnnonnx/";
        
        public static string OutputFolderFasterRCNNONNX { get; set; } = "output_fasterrcnnonnx/";
    }
}
