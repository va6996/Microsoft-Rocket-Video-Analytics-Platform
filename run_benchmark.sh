#!/bin/bash

models=( 2 8 9 10 8,9 8,10 9,10 )

set -o errexit

for filename in ./media/*; do
    name=${filename##*/}
    for model in ${models[@]}; do
        dotnet run --project src/VAP/VideoPipelineCore/ $name sample.txt 1 1 $model car person motorbike
    done || exit 1
done || exit 1
