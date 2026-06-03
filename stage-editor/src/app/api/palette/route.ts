import { NextResponse } from 'next/server';
import { readPalette } from '../../../lib/csv';

export async function GET() {
  return NextResponse.json(readPalette());
}
