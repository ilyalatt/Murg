#!/bin/bash
set -e

bash build_app.sh
rm -rf *.snap
snapcraft
mv *.snap murg.snap
