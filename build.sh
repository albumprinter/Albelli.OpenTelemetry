#!/usr/bin/env bash

FAKE_EXE=packages/build/FAKE/tools/FAKE.exe

OS=${OS:-"unknown"}
run() {
  if [[ "$OS" != "Windows_NT" ]]
  then
    mono "$@"
  else
    "$@"
  fi
}

./paket.sh restore
run $FAKE_EXE build.fsx "$@" "parallel-jobs=1"