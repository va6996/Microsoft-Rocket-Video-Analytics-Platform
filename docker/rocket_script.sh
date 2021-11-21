git clone https://github.com/va6996/Microsoft-Rocket-Video-Analytics-Platform.git
cd Microsoft-Rocket-Video-Analytics-Platform
chmod +x Config.sh
bash Config.sh

cd src/VAP/VideoPipelineCore
dotnet build
