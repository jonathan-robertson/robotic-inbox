#!/bin/sh
references_folder=./references
harmony_filename="0Harmony.dll"
harmony_folder="/c/Program Files (x86)/Steam/steamapps/common/7 Days to Die Dedicated Server/Mods/0_TFP_Harmony"
managed_folder="/c/Program Files (x86)/Steam/steamapps/common/7 Days to Die Dedicated Server/7DaysToDieServer_Data/Managed"

cd $references_folder
for file in *; do
    if [ -f "$file" ]; then
        if [ "$file" = "$harmony_filename" ]; then
            cp "$harmony_folder/$file" ./
        else
            cp "$managed_folder/$file" ./
        fi
    fi
done