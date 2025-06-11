#!/usr/bin/env bash

export VERSION=$(cat .version)
echo Adding git tag with version v${VERSION}
git tag v${VERSION} -f
git push origin v${VERSION}
