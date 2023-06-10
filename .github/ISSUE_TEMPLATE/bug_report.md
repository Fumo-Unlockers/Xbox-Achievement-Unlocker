name: Bug Report
description: File a bug report
title: "[Bug]: "
labels: ["bug"]
body:
  - type: markdown
    attributes:
      value: |
        Make sure you fill in every section correctly and with as much detail as possible.
  - type: input
    id: Tool-Version
    attributes:
      label: What version of the app were you running?
      placeholder: 1.xx
    validations:
      required: true
  - type: dropdown
    id: Winver
    attributes:
      label: Winver
      description: What Version of windows are you running?
      options:
        - Win10
        - Win11
    validations:
      required: true
  - type: dropdown
    id: have-i-checked
    attributes:
      label: Have I checked if the bug was reported?
      description: Did you search the previous issues in this repo for similar issues?
      options:
        - "Yes"
        - "No"
    validations:
      required: true
  - type: textarea
    id: issue-description
    attributes:
      label: What is the bug?
      description: What did you expect to happen and what actually happened? What were you doing? (include images/video where applicable)
    validations:
      required: true
