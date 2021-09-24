#!/bin/bash

models=( 2 8 9 10 9,10)

for filename in ./media; do
    for model in models; do
        echo "dotnet run --project src/VAP/VideoPipelineCore/ $filename sample.txt 1 1 $model car person motorbike"
done