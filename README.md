# Discrete Logarithm Solver

A distributed implementation of discrete logarithm problem (DLog) using Parcs.Net framework and Kubernetes on Azure.

## Problem

Find `x` such that: `g^x mod p = h` where `p` is a large prime, `g` is a generator, and `h` is an element in Z_p*.

## Technologies
- C#, Parcs.Net
- Kubernetes, Azure

## Algorithm
Uses round-robin to distribute workload among workers:
- Each worker `i` processes only values where `x ≡ i (mod N)`
- Pre-computed factors optimize power calculations
