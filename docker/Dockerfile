FROM nvidia/cuda:10.0-cudnn7-runtime-ubuntu18.04

COPY script.sh script.sh
RUN chmod +x script.sh
RUN ./script.sh
COPY rocket_script.sh rocket_script.sh
RUN chmod +x rocket_script.sh
RUN ./rocket_script.sh
