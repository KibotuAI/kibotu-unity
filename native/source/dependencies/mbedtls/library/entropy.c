/*
 *  Entropy accumulator implementation
 *
 *  Copyright (C) 2006-2015, ARM Limited, All Rights Reserved
 *  SPDX-License-Identifier: Apache-2.0
 *
 *  Licensed under the Apache License, Version 2.0 (the "License"); you may
 *  not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *  http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 *  WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 *  This file is part of mbed TLS (https://tls.mbed.org)
 */

#if !defined(MBEDTLS_CONFIG_FILE)
#include "mbedtls/config.h"
#else
#include MBEDTLS_CONFIG_FILE
#endif

#if defined(MBEDTLS_ENTROPY_C)

#include "mbedtls/entropy.h"
#include "mbedtls/entropy_poll.h"

#include <string.h>

#if defined(MBEDTLS_FS_IO)
#include <stdio.h>
#endif

#if defined(MBEDTLS_SELF_TEST)
#if defined(MBEDTLS_PLATFORM_C)
#include "mbedtls/platform.h"
#else
#include <stdio.h>
#define mixpanel_mbedtls_printf     printf
#endif /* MBEDTLS_PLATFORM_C */
#endif /* MBEDTLS_SELF_TEST */

#if defined(MBEDTLS_HAVEGE_C)
#include "mbedtls/havege.h"
#endif

/* Implementation that should never be optimized out by the compiler */
static void mixpanel_mbedtls_zeroize( void *v, size_t n ) {
    volatile unsigned char *p = v; while( n-- ) *p++ = 0;
}

#define ENTROPY_MAX_LOOP    256     /**< Maximum amount to loop before error */

void mixpanel_mbedtls_entropy_init( mixpanel_mbedtls_entropy_context *ctx )
{
    memset( ctx, 0, sizeof(mixpanel_mbedtls_entropy_context) );

#if defined(MBEDTLS_THREADING_C)
    mixpanel_mbedtls_mutex_init( &ctx->mutex );
#endif

#if defined(MBEDTLS_ENTROPY_SHA512_ACCUMULATOR)
    mixpanel_mbedtls_sha512_starts( &ctx->accumulator, 0 );
#else
    mixpanel_mbedtls_sha256_starts( &ctx->accumulator, 0 );
#endif
#if defined(MBEDTLS_HAVEGE_C)
    mixpanel_mbedtls_havege_init( &ctx->havege_data );
#endif

#if !defined(MBEDTLS_NO_DEFAULT_ENTROPY_SOURCES)
#if !defined(MBEDTLS_NO_PLATFORM_ENTROPY)
    mixpanel_mbedtls_entropy_add_source( ctx, mixpanel_mbedtls_platform_entropy_poll, NULL,
                                MBEDTLS_ENTROPY_MIN_PLATFORM,
                                MBEDTLS_ENTROPY_SOURCE_STRONG );
#endif
#if defined(MBEDTLS_TIMING_C)
    mixpanel_mbedtls_entropy_add_source( ctx, mixpanel_mbedtls_hardclock_poll, NULL,
                                MBEDTLS_ENTROPY_MIN_HARDCLOCK,
                                MBEDTLS_ENTROPY_SOURCE_WEAK );
#endif
#if defined(MBEDTLS_HAVEGE_C)
    mixpanel_mbedtls_entropy_add_source( ctx, mixpanel_mbedtls_havege_poll, &ctx->havege_data,
                                MBEDTLS_ENTROPY_MIN_HAVEGE,
                                MBEDTLS_ENTROPY_SOURCE_STRONG );
#endif
#if defined(MBEDTLS_ENTROPY_HARDWARE_ALT)
    mixpanel_mbedtls_entropy_add_source( ctx, mixpanel_mbedtls_hardware_poll, NULL,
                                MBEDTLS_ENTROPY_MIN_HARDWARE,
                                MBEDTLS_ENTROPY_SOURCE_STRONG );
#endif
#endif /* MBEDTLS_NO_DEFAULT_ENTROPY_SOURCES */
}

void mixpanel_mbedtls_entropy_free( mixpanel_mbedtls_entropy_context *ctx )
{
#if defined(MBEDTLS_HAVEGE_C)
    mixpanel_mbedtls_havege_free( &ctx->havege_data );
#endif
#if defined(MBEDTLS_THREADING_C)
    mixpanel_mbedtls_mutex_free( &ctx->mutex );
#endif
    mixpanel_mbedtls_zeroize( ctx, sizeof( mixpanel_mbedtls_entropy_context ) );
}

int mixpanel_mbedtls_entropy_add_source( mixpanel_mbedtls_entropy_context *ctx,
                        mixpanel_mbedtls_entropy_f_source_ptr f_source, void *p_source,
                        size_t threshold, int strong )
{
    int index, ret = 0;

#if defined(MBEDTLS_THREADING_C)
    if( ( ret = mixpanel_mbedtls_mutex_lock( &ctx->mutex ) ) != 0 )
        return( ret );
#endif

    index = ctx->source_count;
    if( index >= MBEDTLS_ENTROPY_MAX_SOURCES )
    {
        ret = MBEDTLS_ERR_ENTROPY_MAX_SOURCES;
        goto exit;
    }

    ctx->source[index].f_source  = f_source;
    ctx->source[index].p_source  = p_source;
    ctx->source[index].threshold = threshold;
    ctx->source[index].strong    = strong;

    ctx->source_count++;

exit:
#if defined(MBEDTLS_THREADING_C)
    if( mixpanel_mbedtls_mutex_unlock( &ctx->mutex ) != 0 )
        return( MBEDTLS_ERR_THREADING_MUTEX_ERROR );
#endif

    return( ret );
}

/*
 * Entropy accumulator update
 */
static int entropy_update( mixpanel_mbedtls_entropy_context *ctx, unsigned char source_id,
                           const unsigned char *data, size_t len )
{
    unsigned char header[2];
    unsigned char tmp[MBEDTLS_ENTROPY_BLOCK_SIZE];
    size_t use_len = len;
    const unsigned char *p = data;

    if( use_len > MBEDTLS_ENTROPY_BLOCK_SIZE )
    {
#if defined(MBEDTLS_ENTROPY_SHA512_ACCUMULATOR)
        mixpanel_mbedtls_sha512( data, len, tmp, 0 );
#else
        mixpanel_mbedtls_sha256( data, len, tmp, 0 );
#endif
        p = tmp;
        use_len = MBEDTLS_ENTROPY_BLOCK_SIZE;
    }

    header[0] = source_id;
    header[1] = use_len & 0xFF;

#if defined(MBEDTLS_ENTROPY_SHA512_ACCUMULATOR)
    mixpanel_mbedtls_sha512_update( &ctx->accumulator, header, 2 );
    mixpanel_mbedtls_sha512_update( &ctx->accumulator, p, use_len );
#else
    mixpanel_mbedtls_sha256_update( &ctx->accumulator, header, 2 );
    mixpanel_mbedtls_sha256_update( &ctx->accumulator, p, use_len );
#endif

    return( 0 );
}

int mixpanel_mbedtls_entropy_update_manual( mixpanel_mbedtls_entropy_context *ctx,
                           const unsigned char *data, size_t len )
{
    int ret;

#if defined(MBEDTLS_THREADING_C)
    if( ( ret = mixpanel_mbedtls_mutex_lock( &ctx->mutex ) ) != 0 )
        return( ret );
#endif

    ret = entropy_update( ctx, MBEDTLS_ENTROPY_SOURCE_MANUAL, data, len );

#if defined(MBEDTLS_THREADING_C)
    if( mixpanel_mbedtls_mutex_unlock( &ctx->mutex ) != 0 )
        return( MBEDTLS_ERR_THREADING_MUTEX_ERROR );
#endif

    return( ret );
}

/*
 * Run through the different sources to add entropy to our accumulator
 */
static int entropy_gather_internal( mixpanel_mbedtls_entropy_context *ctx )
{
    int ret, i, have_one_strong = 0;
    unsigned char buf[MBEDTLS_ENTROPY_MAX_GATHER];
    size_t olen;

    if( ctx->source_count == 0 )
        return( MBEDTLS_ERR_ENTROPY_NO_SOURCES_DEFINED );

    /*
     * Run through our entropy sources
     */
    for( i = 0; i < ctx->source_count; i++ )
    {
        if( ctx->source[i].strong == MBEDTLS_ENTROPY_SOURCE_STRONG )
            have_one_strong = 1;

        olen = 0;
        if( ( ret = ctx->source[i].f_source( ctx->source[i].p_source,
                        buf, MBEDTLS_ENTROPY_MAX_GATHER, &olen ) ) != 0 )
        {
            return( ret );
        }

        /*
         * Add if we actually gathered something
         */
        if( olen > 0 )
        {
            entropy_update( ctx, (unsigned char) i, buf, olen );
            ctx->source[i].size += olen;
        }
    }

    if( have_one_strong == 0 )
        return( MBEDTLS_ERR_ENTROPY_NO_STRONG_SOURCE );

    return( 0 );
}

/*
 * Thread-safe wrapper for entropy_gather_internal()
 */
int mixpanel_mbedtls_entropy_gather( mixpanel_mbedtls_entropy_context *ctx )
{
    int ret;

#if defined(MBEDTLS_THREADING_C)
    if( ( ret = mixpanel_mbedtls_mutex_lock( &ctx->mutex ) ) != 0 )
        return( ret );
#endif

    ret = entropy_gather_internal( ctx );

#if defined(MBEDTLS_THREADING_C)
    if( mixpanel_mbedtls_mutex_unlock( &ctx->mutex ) != 0 )
        return( MBEDTLS_ERR_THREADING_MUTEX_ERROR );
#endif

    return( ret );
}

int mixpanel_mbedtls_entropy_func( void *data, unsigned char *output, size_t len )
{
    int ret, count = 0, i, done;
    mixpanel_mbedtls_entropy_context *ctx = (mixpanel_mbedtls_entropy_context *) data;
    unsigned char buf[MBEDTLS_ENTROPY_BLOCK_SIZE];

    if( len > MBEDTLS_ENTROPY_BLOCK_SIZE )
        return( MBEDTLS_ERR_ENTROPY_SOURCE_FAILED );

#if defined(MBEDTLS_THREADING_C)
    if( ( ret = mixpanel_mbedtls_mutex_lock( &ctx->mutex ) ) != 0 )
        return( ret );
#endif

    /*
     * Always gather extra entropy before a call
     */
    do
    {
        if( count++ > ENTROPY_MAX_LOOP )
        {
            ret = MBEDTLS_ERR_ENTROPY_SOURCE_FAILED;
            goto exit;
        }

        if( ( ret = entropy_gather_internal( ctx ) ) != 0 )
            goto exit;

        done = 1;
        for( i = 0; i < ctx->source_count; i++ )
            if( ctx->source[i].size < ctx->source[i].threshold )
                done = 0;
    }
    while( ! done );

    memset( buf, 0, MBEDTLS_ENTROPY_BLOCK_SIZE );

#if defined(MBEDTLS_ENTROPY_SHA512_ACCUMULATOR)
    mixpanel_mbedtls_sha512_finish( &ctx->accumulator, buf );

    /*
     * Reset accumulator and counters and recycle existing entropy
     */
    memset( &ctx->accumulator, 0, sizeof( mixpanel_mbedtls_sha512_context ) );
    mixpanel_mbedtls_sha512_starts( &ctx->accumulator, 0 );
    mixpanel_mbedtls_sha512_update( &ctx->accumulator, buf, MBEDTLS_ENTROPY_BLOCK_SIZE );

    /*
     * Perform second SHA-512 on entropy
     */
    mixpanel_mbedtls_sha512( buf, MBEDTLS_ENTROPY_BLOCK_SIZE, buf, 0 );
#else /* MBEDTLS_ENTROPY_SHA512_ACCUMULATOR */
    mixpanel_mbedtls_sha256_finish( &ctx->accumulator, buf );

    /*
     * Reset accumulator and counters and recycle existing entropy
     */
    memset( &ctx->accumulator, 0, sizeof( mixpanel_mbedtls_sha256_context ) );
    mixpanel_mbedtls_sha256_starts( &ctx->accumulator, 0 );
    mixpanel_mbedtls_sha256_update( &ctx->accumulator, buf, MBEDTLS_ENTROPY_BLOCK_SIZE );

    /*
     * Perform second SHA-256 on entropy
     */
    mixpanel_mbedtls_sha256( buf, MBEDTLS_ENTROPY_BLOCK_SIZE, buf, 0 );
#endif /* MBEDTLS_ENTROPY_SHA512_ACCUMULATOR */

    for( i = 0; i < ctx->source_count; i++ )
        ctx->source[i].size = 0;

    memcpy( output, buf, len );

    ret = 0;

exit:
#if defined(MBEDTLS_THREADING_C)
    if( mixpanel_mbedtls_mutex_unlock( &ctx->mutex ) != 0 )
        return( MBEDTLS_ERR_THREADING_MUTEX_ERROR );
#endif

    return( ret );
}

#if defined(MBEDTLS_FS_IO)
int mixpanel_mbedtls_entropy_write_seed_file( mixpanel_mbedtls_entropy_context *ctx, const char *path )
{
    int ret = MBEDTLS_ERR_ENTROPY_FILE_IO_ERROR;
    FILE *f;
    unsigned char buf[MBEDTLS_ENTROPY_BLOCK_SIZE];

    if( ( f = fopen( path, "wb" ) ) == NULL )
        return( MBEDTLS_ERR_ENTROPY_FILE_IO_ERROR );

    if( ( ret = mixpanel_mbedtls_entropy_func( ctx, buf, MBEDTLS_ENTROPY_BLOCK_SIZE ) ) != 0 )
        goto exit;

    if( fwrite( buf, 1, MBEDTLS_ENTROPY_BLOCK_SIZE, f ) != MBEDTLS_ENTROPY_BLOCK_SIZE )
    {
        ret = MBEDTLS_ERR_ENTROPY_FILE_IO_ERROR;
        goto exit;
    }

    ret = 0;

exit:
    fclose( f );
    return( ret );
}

int mixpanel_mbedtls_entropy_update_seed_file( mixpanel_mbedtls_entropy_context *ctx, const char *path )
{
    FILE *f;
    size_t n;
    unsigned char buf[ MBEDTLS_ENTROPY_MAX_SEED_SIZE ];

    if( ( f = fopen( path, "rb" ) ) == NULL )
        return( MBEDTLS_ERR_ENTROPY_FILE_IO_ERROR );

    fseek( f, 0, SEEK_END );
    n = (size_t) ftell( f );
    fseek( f, 0, SEEK_SET );

    if( n > MBEDTLS_ENTROPY_MAX_SEED_SIZE )
        n = MBEDTLS_ENTROPY_MAX_SEED_SIZE;

    if( fread( buf, 1, n, f ) != n )
    {
        fclose( f );
        return( MBEDTLS_ERR_ENTROPY_FILE_IO_ERROR );
    }

    fclose( f );

    mixpanel_mbedtls_entropy_update_manual( ctx, buf, n );

    return( mixpanel_mbedtls_entropy_write_seed_file( ctx, path ) );
}
#endif /* MBEDTLS_FS_IO */

#if defined(MBEDTLS_SELF_TEST)
/*
 * Dummy source function
 */
static int entropy_dummy_source( void *data, unsigned char *output,
                                 size_t len, size_t *olen )
{
    ((void) data);

    memset( output, 0x2a, len );
    *olen = len;

    return( 0 );
}

/*
 * The actual entropy quality is hard to test, but we can at least
 * test that the functions don't cause errors and write the correct
 * amount of data to buffers.
 */
int mixpanel_mbedtls_entropy_self_test( int verbose )
{
    int ret = 0;
    mixpanel_mbedtls_entropy_context ctx;
    unsigned char buf[MBEDTLS_ENTROPY_BLOCK_SIZE] = { 0 };
    unsigned char acc[MBEDTLS_ENTROPY_BLOCK_SIZE] = { 0 };
    size_t i, j;

    if( verbose != 0 )
        mixpanel_mbedtls_printf( "  ENTROPY test: " );

    mixpanel_mbedtls_entropy_init( &ctx );

    /* First do a gather to make sure we have default sources */
    if( ( ret = mixpanel_mbedtls_entropy_gather( &ctx ) ) != 0 )
        goto cleanup;

    ret = mixpanel_mbedtls_entropy_add_source( &ctx, entropy_dummy_source, NULL, 16,
                                      MBEDTLS_ENTROPY_SOURCE_WEAK );
    if( ret != 0 )
        goto cleanup;

    if( ( ret = mixpanel_mbedtls_entropy_update_manual( &ctx, buf, sizeof buf ) ) != 0 )
        goto cleanup;

    /*
     * To test that mixpanel_mbedtls_entropy_func writes correct number of bytes:
     * - use the whole buffer and rely on ASan to detect overruns
     * - collect entropy 8 times and OR the result in an accumulator:
     *   any byte should then be 0 with probably 2^(-64), so requiring
     *   each of the 32 or 64 bytes to be non-zero has a false failure rate
     *   of at most 2^(-58) which is acceptable.
     */
    for( i = 0; i < 8; i++ )
    {
        if( ( ret = mixpanel_mbedtls_entropy_func( &ctx, buf, sizeof( buf ) ) ) != 0 )
            goto cleanup;

        for( j = 0; j < sizeof( buf ); j++ )
            acc[j] |= buf[j];
    }

    for( j = 0; j < sizeof( buf ); j++ )
    {
        if( acc[j] == 0 )
        {
            ret = 1;
            goto cleanup;
        }
    }

cleanup:
    mixpanel_mbedtls_entropy_free( &ctx );

    if( verbose != 0 )
    {
        if( ret != 0 )
            mixpanel_mbedtls_printf( "failed\n" );
        else
            mixpanel_mbedtls_printf( "passed\n" );

        mixpanel_mbedtls_printf( "\n" );
    }

    return( ret != 0 );
}
#endif /* MBEDTLS_SELF_TEST */

#endif /* MBEDTLS_ENTROPY_C */
