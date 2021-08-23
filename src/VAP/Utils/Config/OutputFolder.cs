// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Utils.Config
{
    public static class OutputFolder
    {
        public static string OutputFolderRoot { get; set; } = "output/";

        public static string OutputFolderAll { get; set; } = OutputFolderRoot + "output_all/";

        public static string OutputFolderBGSLine { get; set; } = OutputFolderRoot + "output_bgsline/";

        public static string OutputFolderLtDNN { get; set; } = OutputFolderRoot + "output_ltdnn/";

        public static string OutputFolderCcDNN { get; set; } = OutputFolderRoot + "output_ccdnn/";

        public static string OutputFolderAML { get; set; } = OutputFolderRoot + "output_aml/";

        public static string OutputFolderFrameDNNTF { get; set; } = OutputFolderRoot + "output_framednntf/";

        public static string OutputFolderFrameDNNONNX { get; set; } = OutputFolderRoot + "output_framednnonnx/";
    }
}
