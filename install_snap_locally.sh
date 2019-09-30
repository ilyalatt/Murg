#!/bin/bash
set -e

bash build_snap.sh
sudo snap install --dangerous murg.snap
sudo snap connect murg:mount-observe
sudo snap connect murg:process-control
